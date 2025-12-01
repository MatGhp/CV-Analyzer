using CVAnalyzer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CVAnalyzer.Application.Features.Auth.Queries;

/// <summary>
/// Handler for retrieving user profile information.
/// </summary>
public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileResponse>
{
    private readonly IApplicationDbContext _context;

    public GetUserProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileResponse> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Resumes)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        return new UserProfileResponse(
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            Phone: user.Phone,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            IsEmailVerified: user.IsEmailVerified,
            TotalResumes: user.Resumes.Count
        );
    }
}
