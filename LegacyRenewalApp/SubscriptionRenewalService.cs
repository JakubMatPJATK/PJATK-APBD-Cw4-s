using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IPricingService _pricingService;
        private readonly IBillingGateway _billingGateway;
        
        public SubscriptionRenewalService()
            : this(
                new CustomerRepository(),
                new SubscriptionPlanRepository(),
                new PricingService(
                    new DiscountCalculator(),
                    new SupportFeeCalculator(),
                    new PaymentFeeCalculator(new IPaymentFeeStrategy[]
                    {
                        new CardPaymentStrategy(),
                        new BankTransferPaymentStrategy(),
                        new PayPalPaymentStrategy(),
                        new InvoicePaymentStrategy()
                    }),
                    new TaxCalculator()
                ),
                new LegacyBillingGatewayAdapter()
            )
        {
        }
        
        public SubscriptionRenewalService(
            CustomerRepository customerRepository,
            SubscriptionPlanRepository planRepository,
            IPricingService pricingService,
            IBillingGateway billingGateway)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _pricingService = pricingService;
            _billingGateway = billingGateway;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            ValidateInputs(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            var pricing = _pricingService.CalculatePricing(
                customer, plan, seatCount, normalizedPaymentMethod, includePremiumSupport, useLoyaltyPoints);

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(pricing.BaseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(pricing.DiscountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(pricing.SupportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(pricing.PaymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(pricing.TaxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(pricing.FinalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = pricing.Notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _billingGateway.SaveInvoice(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body = $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} has been prepared. Final amount: {invoice.FinalAmount:F2}.";
                
                _billingGateway.SendEmail(customer.Email, subject, body);
            }

            return invoice;
        }

        private static void ValidateInputs(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            if (customerId <= 0)
                throw new ArgumentException("Customer id must be positive");

            if (string.IsNullOrWhiteSpace(planCode))
                throw new ArgumentException("Plan code is required");

            if (seatCount <= 0)
                throw new ArgumentException("Seat count must be positive");

            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required");
        }
    }
}
