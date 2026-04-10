using System;

namespace LegacyRenewalApp
{
    public interface IPaymentFeeStrategy
    {
        bool IsApplicable(string paymentMethod);
        decimal CalculateFee(decimal amountToCharge);
        string Note { get; }
    }

    public class CardPaymentStrategy : IPaymentFeeStrategy
    {
        public bool IsApplicable(string method) => method == "CARD";
        public decimal CalculateFee(decimal amount) => amount * 0.02m;
        public string Note => "card payment fee; ";
    }

    public class BankTransferPaymentStrategy : IPaymentFeeStrategy
    {
        public bool IsApplicable(string method) => method == "BANK_TRANSFER";
        public decimal CalculateFee(decimal amount) => amount * 0.01m;
        public string Note => "bank transfer fee; ";
    }

    public class PayPalPaymentStrategy : IPaymentFeeStrategy
    {
        public bool IsApplicable(string method) => method == "PAYPAL";
        public decimal CalculateFee(decimal amount) => amount * 0.035m;
        public string Note => "paypal fee; ";
    }

    public class InvoicePaymentStrategy : IPaymentFeeStrategy
    {
        public bool IsApplicable(string method) => method == "INVOICE";
        public decimal CalculateFee(decimal amount) => 0m;
        public string Note => "invoice payment; ";
    }
}