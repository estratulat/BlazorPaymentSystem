using System.Collections.Generic;

namespace BlazorApp3.Shared
{
    public class TransactionsHistoryData
    {
        public IEnumerable<TransactionDto> Transactions { get; set; }
        public int Count { get; set; }
    }
}
