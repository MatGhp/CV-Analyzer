using CVAnalyzer.Application.Common.Constants;
using CVAnalyzer.Domain.Enums;

namespace CVAnalyzer.Application.Common.Extensions;

public static class ResumeStatusExtensions
{
    public static string ToStatusString(this ResumeStatus status) => status switch
    {
        ResumeStatus.Pending => ResumeStatusConstants.Pending,
        ResumeStatus.Processing => ResumeStatusConstants.Processing,
        ResumeStatus.Analyzed => ResumeStatusConstants.Complete,
        ResumeStatus.Failed => ResumeStatusConstants.Failed,
        _ => ResumeStatusConstants.Unknown
    };

    public static int GetProgress(this ResumeStatus status) => status switch
    {
        ResumeStatus.Pending => 0,
        ResumeStatus.Processing => 50,
        ResumeStatus.Analyzed => 100,
        ResumeStatus.Failed => 0,
        _ => 0
    };
}
