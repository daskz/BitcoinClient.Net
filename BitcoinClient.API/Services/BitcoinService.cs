using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Data;
using BitcoinClient.API.Services.BackgroundQueue;
using BitcoinClient.API.Services.Rpc;
using BitcoinClient.API.Services.Rpc.ResultEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BitcoinClient.API.Services
{
    public class BitcoinService : IBitcoinService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly RpcClient _rpcClient;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BitcoinService(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor, RpcClient rpcClient, IBackgroundTaskQueue backgroundTaskQueue, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _rpcClient = rpcClient;
            _backgroundTaskQueue = backgroundTaskQueue;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<List<Wallet>> GetUserWalletsAsync()
        {
            return await _context.Wallets.Where(w => w.User.Id == GetCurrentUserId()).AsNoTracking().ToListAsync();
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext.User.Claims.First(c => c.Type == "Id").Value;
        }

        public async Task<Wallet> CreateWalletAsync()
        {
            var walletId = Guid.NewGuid();
            var responseTask = _rpcClient.Invoke<object>(RpcMethod.createwallet, null, walletId);

            var wallet = _context.Add(new Wallet
            {
                Id = walletId,
                User = await _userManager.FindByIdAsync(GetCurrentUserId()),
                Balance = 0
            }).Entity;

            var response = await responseTask;

            if (response.IsSuccessful)
                _context.SaveChanges();
            else throw new Exception("Underlying API error occured");

            return wallet;
        }

        public async Task<List<Address>> GetWalletAddressesAsync(Guid walletId)
        {
            return await _context.Addresses
                            .Include(a => a.Wallet)
                            .Where(a => a.Wallet.Id == walletId && a.Wallet.User.Id == GetCurrentUserId())
                            .ToListAsync();
        }

        public async Task<Address> CreateWalletAddressAsync(Guid walletId)
        {
            await CheckWalletAccess(walletId);

            var response = await _rpcClient.Invoke<string>(RpcMethod.getnewaddress, walletId);
            if (!response.IsSuccessful)
                throw new ApplicationException(response.Error.Message);

            var address = _context.Add(new Address
            {
                AddressId = response.Result,
                Wallet = await _context.Wallets.FindAsync(walletId),
                CreatedDate = DateTime.Now
            }).Entity;

            _context.SaveChanges();

            await _rpcClient.Invoke<object>(RpcMethod.importaddress, null, address.AddressId, "", false);

            return address;
        }

        public async Task CreateOutputTransaction(Guid walletId, string address, decimal amount)
        {
            await CheckWalletAccess(walletId);

            var response = await _rpcClient.Invoke<string>(RpcMethod.sendtoaddress, walletId, address, amount);
            if (!response.IsSuccessful)
                throw new ApplicationException(response.Error.Message);

            var txId = response.Result;
            var transactionResponse = await _rpcClient.Invoke<GetTransactionResult>(RpcMethod.gettransaction, walletId, txId);

            var fee = Math.Abs(transactionResponse.Result.Fee);
            var time = DateTimeOffset.FromUnixTimeSeconds(transactionResponse.Result.Time);
            var destinationAddress = GetOrCreateAddress(address);
            _context.OutputTransactions.Add(new OutputTransaction
            {
                TxId = txId,
                Wallet = _context.Wallets.Find(walletId),
                Address = destinationAddress,
                Amount = amount,
                Fee = fee,
                Time = time.UtcDateTime
            });

            _context.SaveChanges();
        }

        private Address GetOrCreateAddress(string address)
        {
            var destinationAddress = _context.Addresses.Include(a => a.Wallet).FirstOrDefault(a => a.AddressId == address);
            if (destinationAddress == null)
            {
                destinationAddress = _context.Add(new Address
                {
                    AddressId = address,
                    Wallet = null,
                    CreatedDate = DateTime.Now
                }).Entity;
            }

            return destinationAddress;
        }

        public async Task<List<InputTransaction>> GetLastInputTransactions(Guid walletId, bool includeRequested)
        {
            await CheckWalletAccess(walletId);
            var lastTransactions =  await _context.InputTransactions
                .Where(t => t.Wallet.Id == walletId 
                            && (includeRequested || t.IsRequested == false || t.ConfirmationCount < 3))
                .Include(t => t.Address)
                .Include(t => t.Wallet)
                .ToListAsync();

            foreach (var transaction in lastTransactions.Where(t => !t.IsRequested))
            {
                transaction.IsRequested = true;
            }
            await _context.SaveChangesAsync();
            
            return lastTransactions;
        }

        public void CreateOrUpdateInputTransaction(string txId)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var inputTransactionUpdater = scope.ServiceProvider.GetRequiredService<IInputTransactionUpdater>();
                    await inputTransactionUpdater.UpdateAsync(txId);
                }
            });
        }

        public void UpdateNotConfirmedTransactions()
        {
            var txIds = _context.InputTransactions.Where(t => t.ConfirmationCount < 6).Select(t => t.TxId).ToList();
            foreach (var txId in txIds)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var inputTransactionUpdater = scope.ServiceProvider.GetRequiredService<IInputTransactionUpdater>();
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        if (context.InputTransactions.Where(it => it.TxId == txId).Any(it => it.ConfirmationCount < 6))
                            await inputTransactionUpdater.UpdateAsync(txId);
                    }
                });
            }
        }

        private async Task CheckWalletAccess(Guid walletId)
        {
            var isOwnWallet = _context.Wallets.Include(w => w.User).Any(w => w.Id == walletId && w.User.Id == GetCurrentUserId());
            if (!isOwnWallet)
                throw new InvalidOperationException("Invalid wallet");
        }
    }
}