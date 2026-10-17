using API.DTO.Payment;
using FluentValidation;

namespace API.Validators.Payment
{
    public class PaymentCallbackValidator
        : AbstractValidator<PaymentCallbackDto>
    {
        public PaymentCallbackValidator()
        {
            RuleFor(x => x.ExternalTransactionId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("External transaction ID is required.")
                .Must(id => !string.IsNullOrWhiteSpace(id))
                .WithMessage("External transaction ID cannot contain only whitespace.")
                .MaximumLength(255)
                .WithMessage("External transaction ID cannot exceed 255 characters.");

            RuleFor(x => x.NewStatus)
                .NotNull()
                .WithMessage("Payment status is required.")
                .IsInEnum()
                .WithMessage("Invalid payment status.");
        }
    }
}
