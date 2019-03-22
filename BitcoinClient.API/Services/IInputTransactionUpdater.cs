using System.Collections.Generic;
using System.Threading.Tasks;
using BitcoinClient.API.Services.Rpc.ResultEntities;

namespace BitcoinClient.API.Services
{
    public interface IInputTransactionUpdater
    {
        Task UpdateAsync(string txId);
        Task UpdateAsync(IEnumerable<TransactionInfo> transactions);
    }
}