using System;
using System.Linq;
using System.Threading.Tasks;
using BitcoinClient.API.Services.Rpc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace BitcoinClient.API.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context, UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager, IConfiguration configuration, RpcClient rpcClient)
        {
            roleManager.CreateAsync(new IdentityRole(UserRole.Service)).Wait();
            var user = new IdentityUser
            {
                UserName = "service@svc.svc",
                Email = "service@svc.svc",
            };
            userManager.CreateAsync(user, "servicepassword").Wait();
            userManager.AddToRoleAsync(user, UserRole.Service).Wait();

            var rpcResponse = rpcClient.Invoke<object>(RpcMethod.createwallet, null, configuration["NodeConfig:DefaultWallet"]).Result;
            if (!rpcResponse.IsSuccessful && rpcResponse.Error.Code == RpcErrorCode.RPC_WALLET_ERROR)
            {
                rpcClient.Invoke<object>(RpcMethod.loadwallet, null, configuration["NodeConfig:DefaultWallet"]).Wait();
            } else if(!rpcResponse.IsSuccessful) throw new ApplicationException(rpcResponse.Error.Message);

            foreach (var walletId in context.Wallets.Select(w => w.Id))
            {
                rpcClient.Invoke<object>(RpcMethod.loadwallet, null, walletId).Wait();
            }
        }
    }
}