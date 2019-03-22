using System;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Dtos;
using BitcoinClient.API.Services;
using BitcoinClient.API.Services.BackgroundQueue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BitcoinClient.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            return Ok(wallets.Select(w => new
            {
                w.Id,
                w.Balance
            }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateWallet()
        {
            var wallet = await _bitcoinService.CreateWalletAsync();
            return Ok(new
            {
                wallet.Id,
                wallet.Balance
            });
        }

        [HttpGet("{walletId}/addresses")]
        public async Task<IActionResult> GetAddresses(Guid walletId)
        {
            var addresses = await _bitcoinService.GetWalletAddressesAsync(walletId);
            return Ok(addresses.Select(a => new
            {
                Address = a.AddressId,
                a.CreatedDate
            }));
        }

        [HttpPost("{walletId}/addresses")]
        public async Task<IActionResult> CreateAddress(Guid walletId)
        {
            var address = await _bitcoinService.CreateWalletAddressAsync(walletId);
            return Ok(new
            {
                Address = address.AddressId,
                address.CreatedDate
            });
        }

        [HttpPost("{walletId}/transactions")]
        public async Task<IActionResult> CreateOutputTransaction(Guid walletId, OutputTransactionDto dto)
        {
            await _bitcoinService.CreateOutputTransaction(walletId, dto.Address, dto.Amount);
            return Ok();
        }

        [HttpGet("{walletId}/transactions")]
        public async Task<IActionResult> GetLastInputTransactions(Guid walletId, bool includeRequested = false)
        {
            var inputTransactions = await _bitcoinService.GetLastInputTransactions(walletId, includeRequested);
            return Ok(inputTransactions.Select(t => new
            {
                t.TxId,
                t.Time,
                Address = t.Address.AddressId,
                WalletId = t.Wallet.Id,
                t.Amount,
                t.ConfirmationCount
            }));
        }

        [Authorize(Roles = UserRole.Service)]
        [HttpPut("transactions/{txId}")]
        public IActionResult NotifyWallet(string txId)
        {
            _bitcoinService.CreateOrUpdateInputTransaction(txId);
            return Ok();
        }

        [Authorize(Roles = UserRole.Service)]
        [HttpPut("/api/blocks/{blockHash}")]
        public IActionResult NotifyBlock(string blockHash)
        {
            _bitcoinService.UpdateNotConfirmedTransactions();
            return Ok(blockHash);
        }
    }
}