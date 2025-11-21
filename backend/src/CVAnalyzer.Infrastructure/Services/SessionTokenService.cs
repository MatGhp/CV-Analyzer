using CVAnalyzer.Application.Common.Interfaces;

namespace CVAnalyzer.Infrastructure.Services;

public class SessionTokenService : ISessionTokenService
{
    private const string GuestPrefix = "guest-";
    
    public string GenerateSessionToken()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var randomPart = Guid.NewGuid().ToString("N")[..12]; // 12 chars
        return $"{GuestPrefix}{timestamp}-{randomPart}";
    }
    
    public bool IsValidSessionToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
            
        if (!token.StartsWith(GuestPrefix))
            return false;
            
        // Format: guest-{timestamp}-{random}
        var parts = token.Split('-');
        return parts.Length == 3 && 
               long.TryParse(parts[1], out _) && 
               parts[2].Length == 12;
    }
    
    public bool IsAnonymousUser(string userId)
    {
        return !string.IsNullOrWhiteSpace(userId) && 
               userId.StartsWith(GuestPrefix);
    }
}
