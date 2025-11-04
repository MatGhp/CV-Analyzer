using FluentValidation;

namespace CVAnalyzer.Application.Features.Resumes.Commands;

public class UploadResumeCommandValidator : AbstractValidator<UploadResumeCommand>
{
    public UploadResumeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required")
            .MaximumLength(255).WithMessage("FileName must not exceed 255 characters");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("FileStream is required");
    }
}
