using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Data;
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
        private readonly RpcClient _client;

        public BitcoinService(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor, RpcClient client)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _client = client;
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

                var result = _client.Invoke("createwallet", null, wallet.Id);

                if (result.IsSuccessful)
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
                var result = _client.Invoke("getnewaddress", walletId.ToString());
                if(!result.IsSuccessful)
                    throw new ApplicationException();

                var address = _context.Add(new Address
                {
                    AddressId = result.Result["result"].Value<string>(),
                    Wallet = await _context.Wallets.FindAsync(walletId),
                    CreatedDate = DateTime.Now
                }).Entity;

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
                var transactionResult = _client.Invoke("sendtoaddress", walletId.ToString(), address, amount);
                if(!transactionResult.IsSuccessful)
                    throw new ApplicationException(transactionResult.Result["error"].Value<string>());

                var txId = transactionResult.Result["result"].Value<string>();
                var transactionInfo = _client.Invoke("gettransaction", walletId.ToString(), txId).Result["result"];

                var fee = Math.Abs(transactionInfo.Value<decimal>("fee"));
                var time = DateTimeOffset.FromUnixTimeSeconds(transactionInfo.Value<long>("time"));
                var destinationAddress = _context.Addresses.FirstOrDefault(a => a.AddressId == address);
                if (destinationAddress == null)
                {
                    destinationAddress = _context.Add(new Address
                    {
                        AddressId = address,
                        Wallet = null,
                        CreatedDate = DateTime.Now
                    }).Entity;
                }
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

        public async Task<List<InputTransaction>> GetLastInputTransactions(bool includeRequested)
        {
            var currentUser = await GetCurrentUser();
            var lastTransactions =  await _context.InputTransactions
                .Where(t => t.Wallet.User.Id == currentUser.Id 
                            && (includeRequested || t.IsRequested == false || t.ConfirmationCount < 3))
                .Include(t => t.Address)
                .Include(t => t.Wallet)
                .AsNoTracking()
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
            await CreateOrUpdateTransaction(txId);
        }

        private async Task CreateOrUpdateTransaction(string txId)
        {
            var result = _client.Invoke("gettransaction", "miner", txId, true);
            if (!result.IsSuccessful) throw new ApplicationException(result.Error);

            var confirmations = result.Result["result"].Value<int>("confirmations");
            var time = DateTimeOffset.FromUnixTimeSeconds(result.Result["result"].Value<long>("time"));
            var inputTransactionJson =
                result.Result["result"]["details"].Where(d => d["category"].Value<string>() == "receive");

            var transactions = inputTransactionJson.Select(it =>
            {
                var addressId = it["address"].Value<string>();
                var inputTransaction = _context.InputTransactions
                    .Include(t => t.Address)
                    .Include(t => t.Wallet)
                    .SingleOrDefault(t => t.Address.AddressId == addressId && t.TxId == txId);
                if (inputTransaction == null)
                {
                    var address = _context.Addresses.Include(a => a.Wallet).Single(a => a.AddressId == addressId);
                    inputTransaction = new InputTransaction
                    {
                        TxId = txId,
                        Address = address,
                        Wallet = address.Wallet,
                        Amount = it["amount"].Value<decimal>(),
                        ConfirmationCount = confirmations,
                        Time = time.UtcDateTime
                    };
                }

                inputTransaction.ConfirmationCount = confirmations;
                var balance = _client.Invoke("getbalance", inputTransaction.Wallet.Id.ToString()).Result["result"].Value<decimal>();
                inputTransaction.Wallet.Balance = balance;
                return inputTransaction;
            });
            _context.UpdateRange(transactions);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateNotConfirmedTransactions()
        {
            var txIds = _context.InputTransactions.Where(t => t.ConfirmationCount < 6).Select(t => t.TxId).ToList();
            foreach (var txId in txIds)
            {
                await CreateOrUpdateTransaction(txId);
            }
        }

        private async Task CheckWalletAccess(Guid walletId)
        {
            var currentUser = await GetCurrentUser();
            var isOwnWallet = _context.Wallets.Include(w => w.User).Any(w => w.Id == walletId && w.User.Id == currentUser.Id);
            if (!isOwnWallet)
                throw new InvalidOperationException();
        }
    }
}