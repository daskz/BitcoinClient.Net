using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitcoinClient.API.Services.Rpc.ResultEntities
{

    public class ListSinceBlockResult
    {
        public List<TransactionResult> Transactions { get; set; }
        public string Lastblock { get; set; }
    }

    public class TransactionResult
    {
        public string Address { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public long Confirmations { get; set; }
        public string TxId { get; set; }
        public int Time { get; set; }
    }
}
