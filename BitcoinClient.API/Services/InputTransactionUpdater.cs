using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Data;
using BitcoinClient.API.Services.Rpc;
using BitcoinClient.API.Services.Rpc.ResultEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BitcoinClient.API.Services
{
    public class InputTransactionUpdater : IInputTransactionUpdater
    {
        private readonly RpcClient _rpcClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InputTransactionUpdater> _logger;

        public InputTransactionUpdater(RpcClient rpcClient, ApplicationDbContext context, ILogger<InputTransactionUpdater> logger)
        {
            _rpcClient = rpcClient;
            _context = context;
            _logger = logger;
        }

        public async Task UpdateAsync(string txId)
        {
            var response = await _rpcClient.Invoke<GetTransactionResult>(RpcMethod.gettransaction, null, txId, true);
            if (!response.IsSuccessful) throw new ApplicationException(response.Error.Message);

            var receivedTransactions = response
                .Result
                .Details
                .Where(d => d.Category == TransactionCategory.receive.ToString())
                .Select(rt => new TransactionInfo
                {
                    TxId = txId,
                    Confirmations = response.Result.Confirmations,
                    Time = response.Result.Time,
                    Address = rt.Address,
                    Amount = rt.Amount,
                    Category = rt.Category
                });

            await CreateOrUpdateTransaction(receivedTransactions);
        }

        public async Task UpdateAsync(IEnumerable<TransactionInfo> transactions)
        {
            var inputTransactions = transactions
                .Where(rt => _context.Addresses.Any(a => a.AddressId == rt.Address));

            await CreateOrUpdateTransaction(inputTransactions);
        }

        private async Task CreateOrUpdateTransaction(IEnumerable<TransactionInfo> transactionInfos)
        {
            foreach (var transactionInfo in transactionInfos)
            {
                try
                {
                    var inputTransaction = _context.InputTransactions
                        .Include(t => t.Wallet)
                        .SingleOrDefault(t => t.Address.AddressId == transactionInfo.Address && t.TxId == transactionInfo.TxId);

                    if (inputTransaction == null)
                    {
                        var address = _context.Addresses.Include(a => a.Wallet).Single(a => a.AddressId == transactionInfo.Address);
                        inputTransaction = new InputTransaction
                        {
                            TxId = transactionInfo.TxId,
                            Address = address,
                            Wallet = address.Wallet,
                            Amount = transactionInfo.Amount,
                            ConfirmationCount = transactionInfo.Confirmations,
                            Time = DateTimeOffset.FromUnixTimeSeconds(transactionInfo.Time).UtcDateTime
                        };
                    }

                    inputTransaction.ConfirmationCount = transactionInfo.Confirmations;
                    if (inputTransaction.ConfirmationCount > 0)
                    {
                        var balanceResponse = await _rpcClient.Invoke<decimal>(RpcMethod.getbalance, inputTransaction.Wallet.Id);
                        if (balanceResponse.IsSuccessful)
                            inputTransaction.Wallet.Balance = balanceResponse.Result;
                    }

                    _context.Update(inputTransaction);
                    await _context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while saving input transaction", transactionInfo.TxId);
                }
            }
        }
    }
}
