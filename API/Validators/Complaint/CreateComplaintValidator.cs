using API.DTO.Complaint;
using FluentValidation;


namespace API.Validators.Complaint
{
    public class CreateComplaintValidator
        : AbstractValidator<CreateComplaintDto>
    {
        public CreateComplaintValidator()
        {
            RuleFor(x => x.Reason)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Complaint reason is required.")
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .WithMessage("Complaint reason cannot contain only whitespace.")
                .MaximumLength(150)
                .WithMessage("Complaint reason cannot exceed 150 characters.");

            RuleFor(x => x.Description)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Complaint description is required.")
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .WithMessage("Complaint description cannot contain only whitespace.")
                .MaximumLength(2000)
                .WithMessage("Complaint description cannot exceed 2000 characters.");

            RuleFor(x => x.TargetUserId)
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .When(x => x.TargetUserId != null)
                .WithMessage("Target user ID cannot be empty.");

            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .When(x => x.OrderId.HasValue)
                .WithMessage("Order ID must be greater than 0.");
        }
    }
}
