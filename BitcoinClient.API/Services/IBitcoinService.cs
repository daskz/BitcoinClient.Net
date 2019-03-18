﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BitcoinClient.API.Data;

namespace BitcoinClient.API.Services
{
    public interface IBitcoinService
    {
        Task<List<Wallet>> GetUserWalletsAsync();
        Task<Wallet> CreateWalletAsync();
        Task<List<Address>> GetWalletAddressesAsync(Guid walletId);
        Task<Address> CreateWalletAddressAsync(Guid walletId);
        Task CreateOutputTransaction(Guid walletId, string address, decimal amount);
        Task<List<InputTransaction>> GetLastInputTransactions(bool includeRequested);
        Task CreateOrUpdateInputTransaction(string txId);
        Task UpdateNotConfirmedTransactions();
    }
}