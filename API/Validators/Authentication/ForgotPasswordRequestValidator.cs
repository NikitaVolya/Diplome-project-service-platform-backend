using API.DTO.Authentication;
using FluentValidation;

namespace API.Validators.Authentication
{
    public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequestDto>
    {
        public ForgotPasswordRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")

                .EmailAddress()
                .WithMessage("Invalid email format.");
        }
    }
}
