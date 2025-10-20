using CVAnalyzer.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(ILogger<BlobStorageService> logger)
    {
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading file {FileName}", fileName);
        await Task.CompletedTask;
        return $"https://storage.example.com/resumes/{fileName}";
    }

    public async Task<Stream> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading from {BlobUrl}", blobUrl);
        await Task.CompletedTask;
        return new MemoryStream();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting {BlobUrl}", blobUrl);
        await Task.CompletedTask;
    }
}
