using API.DTO.Review;
using FluentValidation;

namespace API.Validators.Review
{
    public class UpdateReviewValidator
        : AbstractValidator<UpdateReviewDto>
    {
        public UpdateReviewValidator()
        {
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
