using System;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Data;
using BitcoinClient.API.Services.BackgroundQueue;
using BitcoinClient.API.Services.Rpc;
using BitcoinClient.API.Services.Rpc.ResultEntities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BitcoinClient.API.Services.BlockSync
{
    public class BlockSynchronizer : IBlockSynchronizer
    {
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;
        private readonly RpcClient _rpcClient;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BlockSynchronizer(ILogger<BlockSynchronizer> logger, ApplicationDbContext context, RpcClient rpcClient, IBackgroundTaskQueue backgroundTaskQueue, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _context = context;
            _rpcClient = rpcClient;
            _backgroundTaskQueue = backgroundTaskQueue;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Execute()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "Execute started");

                var currentBlock = await GetCurrentBlock();

                var sinceBlockResponse = await _rpcClient.Invoke<ListSinceBlockResult>(RpcMethod.listsinceblock, null, currentBlock.Hash, 1, true);
                if (!sinceBlockResponse.IsSuccessful)
                {
                    _logger.LogError($"{nameof(BlockSynchronizer)}: {sinceBlockResponse.Error.Message}");
                    return;
                };

                var transactionSinceBlocks = sinceBlockResponse.Result.Transactions.Where(t => t.Category == TransactionCategory.receive.ToString()).ToList();
                _logger.Log(LogLevel.Debug, $"BlockIndex {currentBlock.Index}, {transactionSinceBlocks.Count} transactions found");

                if (transactionSinceBlocks.Any())
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var inputTransactionUpdater = scope.ServiceProvider.GetRequiredService<IInputTransactionUpdater>();
                            await inputTransactionUpdater.UpdateAsync(transactionSinceBlocks);
                        }
                    });

                if (sinceBlockResponse.Result.Lastblock != currentBlock.Hash)
                    await SaveLastBlock(sinceBlockResponse.Result.Lastblock);

                await _context.SaveChangesAsync();
                _logger.Log(LogLevel.Debug, "Execute completed");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error on Block Sync occured");
            }
        }

        private async Task SaveLastBlock(string lastBlockHash)
        {
            var block = await _rpcClient.Invoke<GetBlockResult>(RpcMethod.getblock, null, lastBlockHash);
            _context.SyncBlocks.Add(new SyncBlock
            {
                Index = block.Result.Height,
                Hash = lastBlockHash,
                CreatedDate = DateTime.UtcNow
            });
        }

        private async Task<SyncBlock> GetCurrentBlock()
        {
            var currentBlock = _context.SyncBlocks.SingleOrDefault(b => b.Index == _context.SyncBlocks.Max(sb => sb.Index));
            if (currentBlock != null) return currentBlock;

            var blockCountResponse = await _rpcClient.Invoke<long>(RpcMethod.getblockcount);
            if (!blockCountResponse.IsSuccessful) throw new ApplicationException(blockCountResponse.Error.Message);
            var blockIndex = blockCountResponse.Result;

            var blockHashResponse = await _rpcClient.Invoke<string>(RpcMethod.getblockhash, null, blockIndex);
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
}
