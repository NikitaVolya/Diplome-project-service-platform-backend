using API.DTO.Authentication;
using FluentValidation;

namespace API.Validators.Authentication
{
    public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestDtoValidator()
        {
            
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")

                .EmailAddress()
                .WithMessage("Invalid email format.");

            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Username is required.")

                .MinimumLength(3)
                .WithMessage("Username must be at least 3 characters long.")

                .MaximumLength(20)
                .WithMessage("Username must not exceed 20 characters.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.")

                .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters long.")

                .Matches(@"[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter.")

                .Matches(@"[a-z]")
                .WithMessage("Password must contain at least one lowercase letter.")

                .Matches(@"[0-9]")
                .WithMessage("Password must contain at least one digit.")

                .Matches(@"[\W_]")
                .WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required.");

        }
    }
}
