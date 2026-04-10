using System;

namespace LegacyRenewalApp
{
    public interface IPricingService
    {
        PricingResult CalculatePricing(Customer customer, SubscriptionPlan plan, int seatCount, string paymentMethod, bool includePremiumSupport, bool useLoyaltyPoints);
    }

    public class PricingService : IPricingService
    {
        private readonly IDiscountCalculator _discountCalculator;
        private readonly ISupportFeeCalculator _supportFeeCalculator;
        private readonly IPaymentFeeCalculator _paymentFeeCalculator;
        private readonly ITaxCalculator _taxCalculator;

        public PricingService(
            IDiscountCalculator discountCalculator,
            ISupportFeeCalculator supportFeeCalculator,
            IPaymentFeeCalculator paymentFeeCalculator,
            ITaxCalculator taxCalculator)
        {
            _discountCalculator = discountCalculator;
            _supportFeeCalculator = supportFeeCalculator;
            _paymentFeeCalculator = paymentFeeCalculator;
            _taxCalculator = taxCalculator;
        }

        public PricingResult CalculatePricing(Customer customer, SubscriptionPlan plan, int seatCount, string paymentMethod, bool includePremiumSupport, bool useLoyaltyPoints)
        {
            var result = new PricingResult();

            // 1. Base Amount
            result.BaseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;

            // 2. Discounts
            var discount = _discountCalculator.CalculateDiscounts(customer, plan, seatCount, result.BaseAmount, useLoyaltyPoints);
            result.DiscountAmount = discount.TotalDiscount;
            result.Notes += discount.Notes;

            decimal subtotalAfterDiscount = result.BaseAmount - result.DiscountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                result.Notes += "minimum discounted subtotal applied; ";
            }

            // 3. Support Fee
            var support = _supportFeeCalculator.Calculate(plan.Code, includePremiumSupport);
            result.SupportFee = support.Fee;
            result.Notes += support.Note;

            // 4. Payment Fee
            var payment = _paymentFeeCalculator.Calculate(paymentMethod, subtotalAfterDiscount + result.SupportFee);
            result.PaymentFee = payment.Fee;
            result.Notes += payment.Note;

            // 5. Taxes
            decimal taxBase = subtotalAfterDiscount + result.SupportFee + result.PaymentFee;
            result.TaxAmount = _taxCalculator.CalculateTax(taxBase, customer.Country);

            // 6. Final Amount
            decimal finalAmount = taxBase + result.TaxAmount;
            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                result.Notes += "minimum invoice amount applied; ";
            }

            result.FinalAmount = finalAmount;
            return result;
        }
    }
}