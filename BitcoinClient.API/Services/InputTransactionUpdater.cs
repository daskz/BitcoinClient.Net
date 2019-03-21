using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Data;
using BitcoinClient.API.Services.Rpc;
using BitcoinClient.API.Services.Rpc.ResultEntities;
using Microsoft.EntityFrameworkCore;

namespace BitcoinClient.API.Services
{
    public class InputTransactionUpdater : IInputTransactionUpdater
    {
        private readonly RpcClient _rpcClient;
        private readonly ApplicationDbContext _context;

        public InputTransactionUpdater(RpcClient rpcClient, ApplicationDbContext context)
        {
            _rpcClient = rpcClient;
            _context = context;
        }

        public async Task Update(string txId)
        {
            var response = _rpcClient.Invoke<GetTransactionResult>(RpcMethod.gettransaction, null, txId, true);
            if (!response.IsSuccessful) throw new ApplicationException(response.Error.Message);

            var receivedTransactions = response.Result.Details.Where(d => d.Category == TransactionCategory.receive.ToString());
            Update(receivedTransactions.Select(rt => new TransactionResult
            {
                TxId = txId,
                Confirmations = response.Result.Confirmations,
                Time = response.Result.Time,
                Address = rt.Address,
                Amount = rt.Amount,
                Category = rt.Category
            }).ToList());

            await _context.SaveChangesAsync();
        }

        public void Update(List<TransactionResult> transactions)
        {
            var inputTransactions = transactions
                .Where(rt => _context.Addresses.Any(a => a.AddressId == rt.Address))
                .Select(CreateOrUpdateTransaction);

            _context.UpdateRange(inputTransactions);
        }

        private InputTransaction CreateOrUpdateTransaction(TransactionResult transactionResult)
        {
            var inputTransaction = _context.InputTransactions
                .Include(t => t.Address)
                .Include(t => t.Wallet)
                .SingleOrDefault(t => t.Address.AddressId == transactionResult.Address && t.TxId == transactionResult.TxId);

            if (inputTransaction == null)
            {
                var address = _context.Addresses.Include(a => a.Wallet).Single(a => a.AddressId == transactionResult.Address);
                inputTransaction = new InputTransaction
                {
                    TxId = transactionResult.TxId,
                    Address = address,
                    Wallet = address.Wallet,
                    Amount = transactionResult.Amount,
                    ConfirmationCount = transactionResult.Confirmations,
                    Time = DateTimeOffset.FromUnixTimeSeconds(transactionResult.Time).UtcDateTime
                };
            }

            inputTransaction.ConfirmationCount = transactionResult.Confirmations;
            if (inputTransaction.ConfirmationCount > 0)
            {
                var balanceResponse = _rpcClient.Invoke<decimal>(RpcMethod.getbalance, inputTransaction.Wallet.Id);
                inputTransaction.Wallet.Balance = balanceResponse.Result;
            }
            return inputTransaction;
        }
    }

    public interface IInputTransactionUpdater
    {
        Task Update(string txId);
        void Update(List<TransactionResult> transactions);
    }
}
