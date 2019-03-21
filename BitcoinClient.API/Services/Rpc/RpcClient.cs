using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace BitcoinClient.API.Services.Rpc
{
    public class RpcClient
    {
        private readonly IConfiguration _configuration;

        public RpcClient(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Invokes JSON-RPC command to bitcoind service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method">Bitcoin-cli method</param>
        /// <param name="walletId">Target wallet, if null servicewallet will be used</param>
        /// <param name="parameters">Command parameters</param>
        /// <returns></returns>
        public RpcResponse<T> Invoke<T>(RpcMethod method, Guid? walletId = null, params object[] parameters)
        {
            var url = _configuration["NodeConfig:Rpc:Url"] + (walletId?.ToString() ?? _configuration["NodeConfig:DefaultWallet"]);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Credentials = new NetworkCredential(_configuration["NodeConfig:Rpc:User"], _configuration["NodeConfig:Rpc:Password"]);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            var data = CreateRpcRequest(method, parameters);
            webRequest.ContentLength = data.Length;
            try
            {
                using (Stream dataStream = webRequest.GetRequestStream())
                {
                    dataStream.Write(data, 0, data.Length);
                }
            }
            catch (WebException exception)
            {
                return new RpcResponse<T> {Error = new RpcResponseError {Message = exception.Message}, IsSuccessful = false};
            }

            try
            {
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (Stream str = webResponse.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(str))
                        {
                            var response = JsonConvert.DeserializeObject<RpcResponse<T>>(sr.ReadToEnd());
                            response.IsSuccessful = true;
                            return response;
                        }
                    }
                }
            }
            catch (WebException exception)
            {
                if (exception.Response == null)
                    return new RpcResponse<T> { Error = new RpcResponseError { Message = exception.Message }, IsSuccessful = false };

                using (Stream str = exception.Response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(str))
                    {
                        var response = JsonConvert.DeserializeObject<RpcResponse<T>>(sr.ReadToEnd());
                        response.IsSuccessful = false;
                        return response;
                    }
                }
            }
        }

        private static byte[] CreateRpcRequest(RpcMethod method, object[] parameters)
        {
            RpcRequest request = new RpcRequest
            {
                Id = 1,
                Method = method,
                Params = parameters
            };

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var json = JsonConvert.SerializeObject(request, serializerSettings);
            return  Encoding.UTF8.GetBytes(json);
        }

        private class RpcRequest
        {
            public int Id { get; set; }
            [JsonConverter(typeof(StringEnumConverter))]
            public RpcMethod Method { get; set; }
            public object[] Params { get; set; }
        }
    }
}
