using API.DTO.Statistic;
using FluentValidation;

namespace API.Validators.Statistic
{
    public class DateRangeValidator : AbstractValidator<DateRangeDto>
    {
        public DateRangeValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required.");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required.")

                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be greater than or equal to start date.")
                .When(x => x.StartDate != default);
        }
    }
}
