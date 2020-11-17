using BlazorApp3.Server.Models;
using BlazorApp3.Shared;

namespace BlazorApp3.Server.Helpers
{
    public static class DomainMapper
    {
        public static TransactionDto ToDto(Transaction transaction)
        {
            return transaction == null ? null : new TransactionDto
            {
                Amount = transaction.Amount,
                Date = transaction.Date,
                DestinationWalletId = transaction.DestinationWalletId,
                Id = transaction.Id,
                SourceWalletId = transaction.SourceWalletId
            };
        }
    }
}
