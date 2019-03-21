using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Data;
using BitcoinClient.API.Services.Rpc;
using BitcoinClient.API.Services.Rpc.ResultEntities;
using Microsoft.Extensions.Logging;

namespace BitcoinClient.API.Services
{
    public class TransactionSynchronizer : ITransactionSynchronizer
    {
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;
        private readonly RpcClient _rpcClient;
        private readonly IInputTransactionUpdater _transactionUpdater;

        public TransactionSynchronizer(ILogger<TransactionSynchronizer> logger, ApplicationDbContext context, RpcClient rpcClient, IInputTransactionUpdater transactionUpdater)
        {
            _logger = logger;
            _context = context;
            _rpcClient = rpcClient;
            _transactionUpdater = transactionUpdater;
        }

        public void UpdateTransactions()
        {
            _logger.Log(LogLevel.Debug, "UpdateTransactions started");

            using (var dbTransaction = _context.Database.BeginTransaction())
            {
                var currentBlock = GetCurrentBlock();

                var sinceBlockResponse = _rpcClient.Invoke<ListSinceBlockResult>(RpcMethod.listsinceblock, null, currentBlock.Hash, 1, true);
                if (!sinceBlockResponse.IsSuccessful) throw new ApplicationException(sinceBlockResponse.Error.Message);

                var transactionSinceBlocks = sinceBlockResponse.Result.Transactions.Where(t => t.Category == TransactionCategory.receive.ToString()).ToList();
                _logger.Log(LogLevel.Debug, $"BlockIndex {currentBlock.Index}, {transactionSinceBlocks.Count} transactions found");
                _transactionUpdater.Update(transactionSinceBlocks);

                if(sinceBlockResponse.Result.Lastblock != currentBlock.Hash)
                    SaveLastBlock(sinceBlockResponse.Result.Lastblock);

                _context.SaveChanges();
                dbTransaction.Commit();
            }
            _logger.Log(LogLevel.Debug, "UpdateTransactions completed");
        }

        private void SaveLastBlock(string lastBlockHash)
        {
            var block = _rpcClient.Invoke<GetBlockResult>(RpcMethod.getblock, null, lastBlockHash);
            _context.SyncBlocks.Add(new SyncBlock
            {
                Index = block.Result.Height,
                Hash = lastBlockHash,
                CreatedDate = DateTime.UtcNow
            });
        }

        private SyncBlock GetCurrentBlock()
        {
            var currentBlock = _context.SyncBlocks.SingleOrDefault(b => b.Index == _context.SyncBlocks.Max(sb => sb.Index));
            if (currentBlock != null) return currentBlock;

            var blockCountResponse = _rpcClient.Invoke<long>(RpcMethod.getblockcount);
            if (!blockCountResponse.IsSuccessful) throw new ApplicationException(blockCountResponse.Error.Message);
            var blockIndex = blockCountResponse.Result;

            var blockHashResponse = _rpcClient.Invoke<string>(RpcMethod.getblockhash, null, blockIndex);
            if (!blockHashResponse.IsSuccessful) throw new ApplicationException(blockHashResponse.Error.Code);

            currentBlock = new SyncBlock
            {
                Index = blockIndex,
                Hash = blockHashResponse.Result,
                CreatedDate = DateTime.UtcNow
            };
            _context.SyncBlocks.Add(currentBlock);

            return currentBlock;
        }
    }

    public interface ITransactionSynchronizer
    {
        void UpdateTransactions();
    }
}
