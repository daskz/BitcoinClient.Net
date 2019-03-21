using System;
using System.Linq;
using BitcoinClient.API.Services;
using BitcoinClient.API.Services.Rpc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace BitcoinClient.API.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context, UserManager<IdentityUser> userManager, IConfiguration configuration, RpcClient rpcClient)
        {
            var user = new IdentityUser
            {
                UserName = "service@svc.svc",
                Email = "service@svc.svc",
            };
            userManager.CreateAsync(user, "servicepassword").Wait();

            var rpcResponse = rpcClient.Invoke<object>(RpcMethod.createwallet, null, configuration["NodeConfig:DefaultWallet"]);
            if (!rpcResponse.IsSuccessful && rpcResponse.Error.Code == RpcErrorCode.RPC_WALLET_ERROR)
            {
                rpcClient.Invoke<object>(RpcMethod.loadwallet, null, configuration["NodeConfig:DefaultWallet"]);
            }
            else throw new ApplicationException();

            foreach (var walletId in context.Wallets.Select(w => w.Id))
            {
                rpcClient.Invoke<object>(RpcMethod.loadwallet, null, walletId);
            }
        }
    }
}