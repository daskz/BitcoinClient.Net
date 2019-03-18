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
        public async Task<IActionResult> GetLastInputTransactions(bool includeRequested = false)
        {
            var inputTransactions = await _bitcoinService.GetLastInputTransactions(includeRequested);
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

        [HttpPost("transactions/{txId}")]
        public async Task<IActionResult> NotifyWallet(string txId)
        {
            await _bitcoinService.CreateOrUpdateInputTransaction(txId);
            return Ok();
        }

        [HttpPost("/api/blocks/{blockHash}")]
        public async Task<IActionResult> NotifyBlock(string blockHash)
        {
            await _bitcoinService.UpdateNotConfirmedTransactions();
            return Ok(blockHash);
        }

        public class OutputTransactionDto
        {
            public decimal Amount { get; set; }
            public string Address { get; set; }
        }
    }
}