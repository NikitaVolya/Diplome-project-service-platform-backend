using FluentValidation;
using API.DTO.Admin;

namespace API.Validators.Admin
{
    public class AddAdminRequestValidator : AbstractValidator<AddAdminRequestDto>
    {
        public AddAdminRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");
        }
    }
}
