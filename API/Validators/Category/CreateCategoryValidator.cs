using API.DTO.Category;
using FluentValidation;

namespace API.Validators.Category
{
    public class CreateCategoryValidator
        : AbstractValidator<CreateCategoryDto>
    {
        public CreateCategoryValidator()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Category name is required.")
                .MaximumLength(100)
                .WithMessage("Category name cannot exceed 100 characters.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Category name cannot contain only whitespace.");

            RuleFor(x => x.ParentCategoryId)
                .GreaterThan(0)
                .When(x => x.ParentCategoryId.HasValue)
                .WithMessage("Parent category ID must be greater than 0.");
        }
    }
}