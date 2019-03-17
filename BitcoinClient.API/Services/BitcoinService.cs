using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            var applicationUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            return await _context.Wallets.Where(w => w.User.Id == applicationUser.Id).ToListAsync();
        }

        public async Task<Wallet> CreateWallet()
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                var wallet = _context.Add(new Wallet
                {
                    User = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User),
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
    }
}