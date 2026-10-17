using API.DTO.OrderMessage;
using FluentValidation;

namespace API.Validators.OrderMessage
{
    public class SendOrderMessageValidator
        : AbstractValidator<SendOrderMessageDto>
    {
        public SendOrderMessageValidator()
        {
            RuleFor(x => x.Text)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Message text is required.")
                .Must(text => !string.IsNullOrWhiteSpace(text))
                .WithMessage("Message cannot contain only whitespace.")
                .MaximumLength(2000)
                .WithMessage("Message cannot exceed 2000 characters.");
        }
    }
}
