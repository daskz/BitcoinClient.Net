namespace BitcoinClient.API.Services.Rpc
{
    public class RpcResponse <T>
    {
        public T Result { get; set; }
        public bool IsSuccessful { get; set; }
        public RpcResponseError Error { get; set; }
        public string Id { get; set; }
    }

    public class RpcResponseError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public enum RpcMethod
    {
        listsinceblock,
        createwallet,
        getnewaddress,
        importaddress,
        sendtoaddress,
        gettransaction,
        getbalance,
        loadwallet
    }
}
