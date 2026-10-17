using API.DTO.Authentication;
using FluentValidation;

namespace API.Validators.Authentication
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequestDto>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email format.");

            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Token is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("New password is required.")

                .MinimumLength(6)
                .WithMessage("New password must be at least 6 characters long.")

                .Matches(@"[A-Z]")
                .WithMessage("New password must contain at least one uppercase letter.")
                
                .Matches(@"[a-z]")
                .WithMessage("New password must contain at least one lowercase letter.")
                 
                .Matches(@"[0-9]")
                .WithMessage("New password must contain at least one digit.")
                
                .Matches(@"[\W_]")
                .WithMessage("New password must contain at least one special character.");
        }
    }
}
