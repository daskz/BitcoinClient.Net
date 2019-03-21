using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Data;
using BitcoinClient.API.Services.Rpc;
using BitcoinClient.API.Services.Rpc.ResultEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace BitcoinClient.API.Services
{
    public class BitcoinService : IBitcoinService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly RpcClient _rpcClient;
        private readonly IInputTransactionUpdater _inputTransactionUpdater;

        public BitcoinService(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor, RpcClient rpcClient, IInputTransactionUpdater inputTransactionUpdater)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _rpcClient = rpcClient;
            _inputTransactionUpdater = inputTransactionUpdater;
        }

        public async Task<List<Wallet>> GetUserWalletsAsync()
        {
            var applicationUser = await GetCurrentUser();
            return await _context.Wallets.Where(w => w.User.Id == applicationUser.Id).ToListAsync();
        }

        private async Task<IdentityUser> GetCurrentUser()
        {
            return await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        }

        public async Task<Wallet> CreateWalletAsync()
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                var wallet = _context.Add(new Wallet
                {
                    User = await GetCurrentUser(),
                    Balance = 0
                }).Entity;

                _context.SaveChanges();

                var response = _rpcClient.Invoke<object>(RpcMethod.createwallet, null, wallet.Id);

                if (response.IsSuccessful)
                    transaction.Commit();

                else throw new Exception("Underlying API error occured");

                return wallet;
            }
        }

        public async Task<List<Address>> GetWalletAddressesAsync(Guid walletId)
        {
            var currentUser = await GetCurrentUser();
            return await _context
                .Addresses
                .Include(a => a.Wallet)
                .Where(a => a.Wallet.Id == walletId && a.Wallet.User.Id == currentUser.Id)
                .ToListAsync();
        }

        public async Task<Address> CreateWalletAddressAsync(Guid walletId)
        {
            await CheckWalletAccess(walletId);

            using (var transaction = _context.Database.BeginTransaction())
            {
                var response = _rpcClient.Invoke<string>(RpcMethod.getnewaddress, walletId);
                if(!response.IsSuccessful)
                    throw new ApplicationException(response.Error.Message);

                var address = _context.Add(new Address
                {
                    AddressId = response.Result,
                    Wallet = await _context.Wallets.FindAsync(walletId),
                    CreatedDate = DateTime.Now
                }).Entity;

                var importResponse = _rpcClient.Invoke<object>(RpcMethod.importaddress, null, address.AddressId, "", false);
                if (!importResponse.IsSuccessful)
                    throw new ApplicationException(importResponse.Error.Message);

                _context.SaveChanges();
                transaction.Commit();

                return address;
            }
        }

        public async Task CreateOutputTransaction(Guid walletId, string address, decimal amount)
        {
            await CheckWalletAccess(walletId);

            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                var response = _rpcClient.Invoke<string>(RpcMethod.sendtoaddress, walletId, address, amount);

                if(!response.IsSuccessful)
                    throw new ApplicationException(response.Error.Message);

                var txId = response.Result;
                var transactionResponse = _rpcClient.Invoke<GetTransactionResult>(RpcMethod.gettransaction, walletId, txId);

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
                dbContextTransaction.Commit();
            }
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

            _context.InputTransactions.UpdateRange(
                lastTransactions.Where(t => !t.IsRequested).Select(t =>
                {
                    t.IsRequested = true;
                    return t;
                }));
            await _context.SaveChangesAsync();
            
            return lastTransactions;
        }

        public async Task CreateOrUpdateInputTransaction(string txId)
        {
            await _inputTransactionUpdater.Update(txId);
        }

        public async Task UpdateNotConfirmedTransactions()
        {
            var txIds = _context.InputTransactions.Where(t => t.ConfirmationCount < 6).Select(t => t.TxId).ToList();
            foreach (var txId in txIds)
            {
                await _inputTransactionUpdater.Update(txId);
            }
        }

        private async Task CheckWalletAccess(Guid walletId)
        {
            var currentUser = await GetCurrentUser();
            var isOwnWallet = _context.Wallets.Include(w => w.User).Any(w => w.Id == walletId && w.User.Id == currentUser.Id);
            if (!isOwnWallet)
                throw new InvalidOperationException("Invalid wallet");
        }
    }
}