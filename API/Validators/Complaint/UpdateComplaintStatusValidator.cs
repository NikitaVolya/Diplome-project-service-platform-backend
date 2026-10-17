using API.DTO.Complaint;
using FluentValidation;


namespace API.Validators.Complaint
{
    public class UpdateComplaintStatusValidator
        : AbstractValidator<UpdateComplaintStatusDto>
    {
        public UpdateComplaintStatusValidator()
        {
            RuleFor(x => x.Status)
                .NotNull()
                .WithMessage("Complaint status is required.")
                .IsInEnum()
                .WithMessage("Invalid complaint status.");
        }
    }
}
