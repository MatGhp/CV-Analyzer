namespace CVAnalyzer.Infrastructure.Options;

public class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    public bool UseKeyVault { get; set; } = false;
    
    public string? Uri { get; set; }
}
