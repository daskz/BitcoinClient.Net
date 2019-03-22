using System.Threading.Tasks;

namespace BitcoinClient.API.Services.BlockSync
{
    public interface IBlockSynchronizer
    {
        Task Execute();
    }
}