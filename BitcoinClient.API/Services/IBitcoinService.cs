using System.Collections.Generic;
using System.Threading.Tasks;
using BitcoinClient.API.Data;

namespace BitcoinClient.API.Services
{
    public interface IBitcoinService
    {
        Task<List<Wallet>> GetUserWalletsAsync();
        Task<Wallet> CreateWallet();
    }
}