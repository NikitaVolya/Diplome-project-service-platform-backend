using API.DTO.Authentication;
using FluentValidation;

namespace API.Validators.Authentication
{

    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator() { 
            
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.");

        }
    }
}
