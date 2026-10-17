using API.DTO.Order;
using FluentValidation;

namespace API.Validators.Order
{
    public abstract class OrderValidatorBase<T> : AbstractValidator<T>
        where T : IOrderInputDto
    {
        protected OrderValidatorBase()
        {
            RuleFor(x => x.Title)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Order title is required.")
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .WithMessage("Order title cannot contain only whitespace.")
                .MaximumLength(150)
                .WithMessage("Order title cannot exceed 150 characters.");

            RuleFor(x => x.Description)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Order description is required.")
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .WithMessage("Order description cannot contain only whitespace.")
                .MaximumLength(2000)
                .WithMessage("Order description cannot exceed 2000 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Order price must be greater than 0.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0)
                .WithMessage("Category ID must be greater than 0.");

            RuleFor(x => x.Address)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Address is required.")
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .WithMessage("Address cannot contain only whitespace.")
                .MaximumLength(500)
                .WithMessage("Address cannot exceed 500 characters.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90.")
                .Must(x => !double.IsNaN(x) && !double.IsInfinity(x))
                .WithMessage("Latitude must be a finite number.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180.")
                .Must(x => !double.IsNaN(x) && !double.IsInfinity(x))
                .WithMessage("Longitude must be a finite number.");
        }
    }
}
