using API.DTO.Review;
using FluentValidation;

namespace API.Validators.Review
{
    public class CreateReviewValidator
        : AbstractValidator<CreateReviewDto>
    {
        public CreateReviewValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("Order ID must be greater than 0.");

            RuleFor(x => x.TargetUserId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Target user ID is required.")
                .Must(id => !string.IsNullOrWhiteSpace(id))
                .WithMessage("Target user ID cannot contain only whitespace.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Rating must be between 1 and 5.");

            RuleFor(x => x.Comment)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Comment is required.")
                .Must(comment => !string.IsNullOrWhiteSpace(comment))
                .WithMessage("Comment cannot contain only whitespace.")
                .MinimumLength(5)
                .WithMessage("Comment must contain at least 5 characters.")
                .MaximumLength(1000)
                .WithMessage("Comment cannot exceed 1000 characters.");
        }
    }
}
