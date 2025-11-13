namespace CVAnalyzer.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string blobUrl, CancellationToken cancellationToken = default);
    Task<string> GenerateSasTokenAsync(string blobUrl, CancellationToken cancellationToken = default);
}
