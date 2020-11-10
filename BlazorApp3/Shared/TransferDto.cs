using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorApp3.Shared
{
    public class TransferDto
    {
        public string SourceWalletId { get; set; }
        public string Username { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
