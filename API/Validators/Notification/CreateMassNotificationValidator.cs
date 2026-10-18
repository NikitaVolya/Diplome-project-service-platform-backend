using API.DTO.Notification;
using FluentValidation;

namespace API.Validators.Notification
{
    public class CreateMassNotificationValidator
        : AbstractValidator<CreateMassNotificationDto>
    {
        public CreateMassNotificationValidator()
        {
            RuleFor(x => x.UserIds)
                .NotNull()
                .WithMessage("User IDs are required.")
                .NotEmpty()
                .WithMessage("At least one user ID is required.");

            RuleForEach(x => x.UserIds)
                .NotEmpty()
                .WithMessage("User ID cannot be empty.")
                .Must(id => !string.IsNullOrWhiteSpace(id))
                .WithMessage("User ID cannot contain only whitespace.");

            RuleFor(x => x.UserIds)
                .Must(ids => ids == null ||
                    ids.Distinct().Count() == ids.Count)
                .WithMessage("User IDs must be unique.");

            RuleFor(x => x.Title)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Notification title is required.")
                .Must(title => !string.IsNullOrWhiteSpace(title))
                .WithMessage("Notification title cannot contain only whitespace.")
                .MaximumLength(100)
                .WithMessage("Notification title cannot exceed 100 characters.");

            RuleFor(x => x.Message)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Notification message is required.")
                .Must(message => !string.IsNullOrWhiteSpace(message))
                .WithMessage("Notification message cannot contain only whitespace.")
                .MaximumLength(1000)
                .WithMessage("Notification message cannot exceed 1000 characters.");
        }
    }
}
