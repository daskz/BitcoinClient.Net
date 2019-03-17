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
            using (var transaction = _context.Database.BeginTransaction())
            {
                var currentUser = await GetCurrentUser();
                var isOwnWallet = _context.Wallets.Include(w => w.User).Any(w => w.Id == walletId && w.User.Id == currentUser.Id);
                if(!isOwnWallet)
                    throw new InvalidOperationException();

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
    }
}