using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Infrastructure.Options;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CVAnalyzer.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private const int DefaultSasExpirationHours = 24;
    private const int DefaultClockSkewMinutes = 5;
    
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly AzureStorageOptions _options;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureStorageOptions> options,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
        _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        _logger = logger;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var blobName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        var contentType = GetContentType(fileName);
        await blobClient.UploadAsync(fileStream, new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);

        var blobUrl = blobClient.Uri.ToString();

        _logger.LogInformation("Uploaded file {FileName} as {BlobName}", fileName, blobName);

        return blobUrl;
    }

    public async Task<bool> DeleteFileAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobName = ExtractBlobNameFromUrl(blobUrl);
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            
            _logger.LogInformation("Deleted blob {BlobName}: {Success}", blobName, response.Value);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob {BlobUrl}", blobUrl);
            return false;
        }
    }

    public async Task<string> GenerateSasTokenAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobName = ExtractBlobNameFromUrl(blobUrl);
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.ContainerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-(_options.SasClockSkewMinutes > 0 ? _options.SasClockSkewMinutes : DefaultClockSkewMinutes)),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(_options.SasExpirationHours > 0 ? _options.SasExpirationHours : DefaultSasExpirationHours)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            if (_options.UseManagedIdentity)
            {
                var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(sasBuilder.StartsOn, sasBuilder.ExpiresOn, cancellationToken);
                var sasToken = sasBuilder.ToSasQueryParameters(userDelegationKey.Value, _containerClient.AccountName).ToString();
                return $"{blobClient.Uri}?{sasToken}";
            }
            else
            {
                throw new InvalidOperationException("SAS token generation requires managed identity. Set UseManagedIdentity=true or use connection string that includes SAS token.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAS token for {BlobUrl}", blobUrl);
            throw;
        }
    }

    private static string ExtractBlobNameFromUrl(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        return uri.Segments.Last().TrimEnd('/');
    }

    private string GetContentType(string fileName)
    {
        if (_contentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            return contentType;
        }
        return "application/octet-stream";
    }
}
