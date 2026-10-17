using API.DTO.Payment;
using FluentValidation;

namespace API.Validators.Payment
{
    public class CreatePaymentValidator
        : AbstractValidator<CreatePaymentDto>
    {
        public CreatePaymentValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("Order ID must be greater than 0.");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Payment amount must be greater than 0.");

            RuleFor(x => x.ExternalTransactionId)
                .Cascade(CascadeMode.Stop)
                .Must(id => !string.IsNullOrWhiteSpace(id))
                .When(x => x.ExternalTransactionId != null)
                .WithMessage("External transaction ID cannot be empty.")

                .MaximumLength(255)
                .WithMessage("External transaction ID cannot exceed 255 characters.");
        }
    }
}
