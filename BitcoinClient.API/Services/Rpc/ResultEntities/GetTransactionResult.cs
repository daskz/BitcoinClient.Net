using System.Collections.Generic;

namespace BitcoinClient.API.Services.Rpc.ResultEntities
{
    public class GetTransactionResult
    {
        public decimal Fee { get; set; }
        public long Confirmations { get; set; }
        public List<GetTransactionResultDetails> Details { get; set; }
        public int Time { get; set; }
    }

    public class GetTransactionResultDetails
    {
        public string Address { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
    }
}