namespace CVAnalyzer.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}
