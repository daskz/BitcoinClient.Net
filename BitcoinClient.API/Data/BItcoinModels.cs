using System;
using Microsoft.AspNetCore.Identity;

namespace BitcoinClient.API.Data
{
    public class Wallet
    {
        public Guid Id { get; set; }
        public decimal Balance { get; set; }
        public IdentityUser User { get; set; }
    }

    public class Address
    {
        public Guid Id { get; set; }
        public string AddressId { get; set; }
        public Wallet Wallet { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class Transaction
    {
        public Guid Id { get; set; }
        public string TxId { get; set; }
        public DateTime Time { get; set; }
        public Address Address { get; set; }
        public Wallet Wallet { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
    }

    public class InputTransaction : Transaction
    {
        public bool IsRequested { get; set; }
        public int ConfirmationCount { get; set; }
    }

    public class OutputTransaction : Transaction
    {

    }
}