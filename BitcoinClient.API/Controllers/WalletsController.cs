using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BitcoinClient.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletsController : ControllerBase
    {
        private readonly IBitcoinService _bitcoinService;

        public WalletsController(IBitcoinService bitcoinService)
        {
            _bitcoinService = bitcoinService;
        }

        [HttpGet]
        public async Task<IActionResult> GetWallets()
        {
            var wallets = await _bitcoinService.GetUserWalletsAsync();
            return Ok(wallets.Select(w => new {w.Id, w.Balance}));
        }

        [HttpPost]
        public async Task<IActionResult> CreateWallet()
        {
            var wallet = await _bitcoinService.CreateWallet();
            return Ok(new { wallet.Id, wallet.Balance });
        }
    }
}