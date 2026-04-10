using System;
using System.Collections.Generic;
using System.Linq;

namespace LegacyRenewalApp
{
    public interface ITaxCalculator
    {
        decimal CalculateTax(decimal taxBase, string country);
    }

    public class TaxCalculator : ITaxCalculator
    {
        public decimal CalculateTax(decimal taxBase, string country)
        {
            decimal taxRate = country switch
            {
                "Poland" => 0.23m,
                "Germany" => 0.19m,
                "Czech Republic" => 0.21m,
                "Norway" => 0.25m,
                _ => 0.20m
            };
            return taxBase * taxRate;
        }
    }

    public interface ISupportFeeCalculator
    {
        (decimal Fee, string Note) Calculate(string planCode, bool includePremiumSupport);
    }

    public class SupportFeeCalculator : ISupportFeeCalculator
    {
        public (decimal Fee, string Note) Calculate(string planCode, bool includePremiumSupport)
        {
            if (!includePremiumSupport) return (0m, string.Empty);

            decimal fee = planCode switch
            {
                "START" => 250m,
                "PRO" => 400m,
                "ENTERPRISE" => 700m,
                _ => 0m
            };
            return (fee, "premium support included; ");
        }
    }

    public interface IDiscountCalculator
    {
        (decimal TotalDiscount, string Notes) CalculateDiscounts(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints);
    }

    public class DiscountCalculator : IDiscountCalculator
    {
        public (decimal TotalDiscount, string Notes) CalculateDiscounts(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            decimal discountAmount = 0m;
            string notes = "";

            if (customer.Segment == "Silver") { discountAmount += baseAmount * 0.05m; notes += "silver discount; "; }
            else if (customer.Segment == "Gold") { discountAmount += baseAmount * 0.10m; notes += "gold discount; "; }
            else if (customer.Segment == "Platinum") { discountAmount += baseAmount * 0.15m; notes += "platinum discount; "; }
            else if (customer.Segment == "Education" && plan.IsEducationEligible) { discountAmount += baseAmount * 0.20m; notes += "education discount; "; }

            if (customer.YearsWithCompany >= 5) { discountAmount += baseAmount * 0.07m; notes += "long-term loyalty discount; "; }
            else if (customer.YearsWithCompany >= 2) { discountAmount += baseAmount * 0.03m; notes += "basic loyalty discount; "; }

            if (seatCount >= 50) { discountAmount += baseAmount * 0.12m; notes += "large team discount; "; }
            else if (seatCount >= 20) { discountAmount += baseAmount * 0.08m; notes += "medium team discount; "; }
            else if (seatCount >= 10) { discountAmount += baseAmount * 0.04m; notes += "small team discount; "; }

            if (useLoyaltyPoints && customer.LoyaltyPoints > 0)
            {
                int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
                discountAmount += pointsToUse;
                notes += $"loyalty points used: {pointsToUse}; ";
            }

            return (discountAmount, notes);
        }
    }

    public interface IPaymentFeeCalculator
    {
        (decimal Fee, string Note) Calculate(string paymentMethod, decimal amountToCharge);
    }

    public class PaymentFeeCalculator : IPaymentFeeCalculator
    {
        private readonly IEnumerable<IPaymentFeeStrategy> _strategies;

        public PaymentFeeCalculator(IEnumerable<IPaymentFeeStrategy> strategies)
        {
            _strategies = strategies;
        }

        public (decimal Fee, string Note) Calculate(string paymentMethod, decimal amountToCharge)
        {
            var strategy = _strategies.FirstOrDefault(s => s.IsApplicable(paymentMethod));
            if (strategy == null)
            {
                throw new ArgumentException("Unsupported payment method");
            }
            return (strategy.CalculateFee(amountToCharge), strategy.Note);
        }
    }
}