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
        public static async Task Initialize(ApplicationDbContext context, UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager, IConfiguration configuration, RpcClient rpcClient)
        {
            await roleManager.CreateAsync(new IdentityRole(UserRole.Service));
            var user = new IdentityUser
            {
                UserName = "service@svc.svc",
                Email = "service@svc.svc",
            };
            await userManager.CreateAsync(user, "servicepassword");
            await userManager.AddToRoleAsync(user, UserRole.Service);

            var rpcResponse = await rpcClient.Invoke<object>(RpcMethod.createwallet, null, configuration["NodeConfig:DefaultWallet"]);
            if (!rpcResponse.IsSuccessful && rpcResponse.Error.Code == RpcErrorCode.RPC_WALLET_ERROR)
            {
                await rpcClient.Invoke<object>(RpcMethod.loadwallet, null, configuration["NodeConfig:DefaultWallet"]);
            } else if(!rpcResponse.IsSuccessful) throw new ApplicationException(rpcResponse.Error.Message);

            foreach (var walletId in context.Wallets.Select(w => w.Id))
            {
                await rpcClient.Invoke<object>(RpcMethod.loadwallet, null, walletId);
            }
        }
    }
}