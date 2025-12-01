using FluentValidation;

namespace CVAnalyzer.Application.Features.Resumes.Commands;

/// <summary>
/// Validator for DeleteResumeCommand.
/// </summary>
public class DeleteResumeCommandValidator : AbstractValidator<DeleteResumeCommand>
{
    public DeleteResumeCommandValidator()
    {
        RuleFor(x => x.ResumeId)
            .NotEmpty()
            .WithMessage("Resume ID is required.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID is required.");
    }
}
