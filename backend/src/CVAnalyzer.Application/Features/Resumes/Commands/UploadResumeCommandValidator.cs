using FluentValidation;

namespace CVAnalyzer.Application.Features.Resumes.Commands;

public class UploadResumeCommandValidator : AbstractValidator<UploadResumeCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };

    public UploadResumeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(HasValidExtension)
            .WithMessage("Only PDF and DOCX files are allowed");

        RuleFor(x => x.FileStream)
            .NotNull()
            .Must(stream => stream.Length > 0)
            .WithMessage("File cannot be empty")
            .Must(stream => stream.Length <= MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024}MB");
    }

    private bool HasValidExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}
