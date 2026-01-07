using Shop_ProjForWeb.Core.Application.Configuration;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Shop_ProjForWeb.Infrastructure.Services;

public class CloudFileStorageService : IFileStorageService
{
    private readonly FileUploadOptions _options;
    private readonly ILogger<CloudFileStorageService> _logger;
    private readonly SemaphoreSlim _semaphore;

    private BlobServiceClient? _azureClient;
    private BlobContainerClient? _azureContainer;

    private AmazonS3Client? _s3Client;
    private readonly string _s3BucketName;

    public CloudFileStorageService(
        IOptions<FileUploadOptions> options,
        ILogger<CloudFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _semaphore = new SemaphoreSlim(
            _options.MaxConcurrentUploads,
            _options.MaxConcurrentUploads);

        _s3BucketName = _options.AwsS3BucketName ?? string.Empty;

        InitializeAzureClient();
        InitializeS3Client();
    }

    #region ===== Public API =====

    public async Task<string> UploadFileAsync(
        IFormFile file,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            ValidateFile(file);

            var fileName = GenerateUniqueFileName(file.FileName);
            var blobName = BuildBlobName(folder, fileName);

            return _options.StorageProvider switch
            {
                StorageProvider.AzureBlob =>
                    await UploadToAzureAsync(file, blobName, cancellationToken),

                StorageProvider.AwsS3 =>
                    await UploadToS3Async(file, blobName, cancellationToken),

                _ => throw new NotSupportedException()
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // ✅ REQUIRED BY INTERFACE
    public async Task<ChunkUploadResult> UploadChunkAsync(
        IFormFile chunk,
        string fileId,
        int chunkNumber,
        int totalChunks,
        string? folder = null)
    {
        ValidateFile(chunk);

        var blobName =
            $"{folder ?? "temp"}/{fileId}/chunk_{chunkNumber:D4}";

        return _options.StorageProvider switch
        {
            StorageProvider.AzureBlob =>
                await UploadChunkToAzureAsync(
                    chunk, blobName, fileId, chunkNumber, totalChunks),

            StorageProvider.AwsS3 =>
                await UploadChunkToS3Async(
                    chunk, blobName, fileId, chunkNumber, totalChunks),

            _ => new ChunkUploadResult
            {
                Success = false,
                ErrorMessage = "Storage provider not supported"
            }
        };
    }

    // ✅ REQUIRED BY INTERFACE
    public Task<string> CompleteChunkedUploadAsync(
        string fileId,
        string fileName,
        string? folder = null)
    {
        var finalBlob =
            BuildBlobName(folder, GenerateUniqueFileName(fileName));

        return _options.StorageProvider switch
        {
            StorageProvider.AzureBlob =>
                CompleteAzureChunkedUploadAsync(fileId, finalBlob),

            StorageProvider.AwsS3 =>
                CompleteS3ChunkedUploadAsync(fileId, finalBlob),

            _ => throw new NotSupportedException()
        };
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        var blobName = ExtractBlobName(filePath);

        return _options.StorageProvider switch
        {
            StorageProvider.AzureBlob =>
                await DeleteFromAzureAsync(blobName),

            StorageProvider.AwsS3 =>
                await DeleteFromS3Async(blobName),

            _ => false
        };
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        var blobName = ExtractBlobName(filePath);

        return _options.StorageProvider switch
        {
            StorageProvider.AzureBlob =>
                await CheckAzureFileExistsAsync(blobName),

            StorageProvider.AwsS3 =>
                await CheckS3FileExistsAsync(blobName),

            _ => false
        };
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string filePath)
    {
        var blobName = ExtractBlobName(filePath);

        return _options.StorageProvider switch
        {
            StorageProvider.AzureBlob =>
                await GetAzureFileMetadataAsync(blobName),

            StorageProvider.AwsS3 =>
                await GetS3FileMetadataAsync(blobName),

            _ => null
        };
    }

    public async Task<Stream> GetFileStreamAsync(string filePath)
    {
        var blobName = ExtractBlobName(filePath);

        return _options.StorageProvider switch
        {
            StorageProvider.AzureBlob =>
                await GetAzureFileStreamAsync(blobName),

            StorageProvider.AwsS3 =>
                await GetS3FileStreamAsync(blobName),

            _ => throw new NotSupportedException()
        };
    }

    public Task<string> GeneratePresignedUrlAsync(
        string filePath,
        int expirationMinutes = 60)
    {
        var blobName = ExtractBlobName(filePath);

        return _options.StorageProvider switch
        {
            StorageProvider.AzureBlob =>
                GenerateAzurePresignedUrlAsync(blobName, expirationMinutes),

            StorageProvider.AwsS3 =>
                GenerateS3PresignedUrlAsync(blobName, expirationMinutes),

            _ => throw new NotSupportedException()
        };
    }

    // ✅ REQUIRED BY INTERFACE
    public async Task<bool> CopyFileAsync(
        string sourcePath,
        string destinationPath)
    {
        var sourceBlob = ExtractBlobName(sourcePath);
        var destBlob = ExtractBlobName(destinationPath);

        return _options.StorageProvider switch
        {
            StorageProvider.AzureBlob =>
                await CopyAzureFileAsync(sourceBlob, destBlob),

            StorageProvider.AwsS3 =>
                await CopyS3FileAsync(sourceBlob, destBlob),

            _ => false
        };
    }

    #endregion

    #region ===== Initialization =====

    private void InitializeAzureClient()
    {
        if (_options.StorageProvider != StorageProvider.AzureBlob)
            return;

        _azureClient = new BlobServiceClient(
            _options.AzureStorageConnectionString);

        _azureContainer =
            _azureClient.GetBlobContainerClient(
                _options.AzureContainerName);

        _azureContainer.CreateIfNotExists(PublicAccessType.Blob);
    }

    private void InitializeS3Client()
    {
        if (_options.StorageProvider != StorageProvider.AwsS3)
            return;

        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(
                _options.AwsRegion ?? "us-east-1")
        };

        _s3Client = new AmazonS3Client(
            _options.AwsAccessKey,
            _options.AwsSecretKey,
            config);
    }

    #endregion

    #region ===== Azure =====

    private async Task<string> UploadToAzureAsync(
        IFormFile file,
        string blobName,
        CancellationToken ct)
    {
        var blob = _azureContainer!.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = file.ContentType },
            cancellationToken: ct);

        return blob.Uri.ToString();
    }

    private async Task<ChunkUploadResult> UploadChunkToAzureAsync(
        IFormFile chunk,
        string blobName,
        string fileId,
        int chunkNumber,
        int totalChunks)
    {
        var blob = _azureContainer!.GetBlobClient(blobName);

        await using var stream = chunk.OpenReadStream();
        await blob.UploadAsync(stream);

        return new ChunkUploadResult
        {
            Success = true,
            FileId = fileId,
            ChunkNumber = chunkNumber,
            TotalChunks = totalChunks,
            UploadedBytes = chunk.Length
        };
    }

    private Task<string> CompleteAzureChunkedUploadAsync(
        string fileId,
        string finalBlobName)
    {
        var blob = _azureContainer!.GetBlobClient(finalBlobName);
        return Task.FromResult(blob.Uri.ToString());
    }

    private async Task<bool> DeleteFromAzureAsync(string blobName)
    {
        var blob = _azureContainer!.GetBlobClient(blobName);
        return await blob.DeleteIfExistsAsync();
    }

    private async Task<bool> CheckAzureFileExistsAsync(string blobName)
    {
        var blob = _azureContainer!.GetBlobClient(blobName);
        return await blob.ExistsAsync();
    }

    private async Task<FileMetadata?> GetAzureFileMetadataAsync(
        string blobName)
    {
        var blob = _azureContainer!.GetBlobClient(blobName);
        var props = await blob.GetPropertiesAsync();

        return new FileMetadata
        {
            Path = blob.Uri.ToString(),
            FileName = Path.GetFileName(blobName),
            ContentType = props.Value.ContentType,
            SizeBytes = props.Value.ContentLength,
            CreatedAt = props.Value.CreatedOn.UtcDateTime,
            ModifiedAt = props.Value.LastModified.UtcDateTime
        };
    }

    private async Task<Stream> GetAzureFileStreamAsync(string blobName)
    {
        var blob = _azureContainer!.GetBlobClient(blobName);
        var res = await blob.DownloadAsync();
        return res.Value.Content;
    }

    private Task<string> GenerateAzurePresignedUrlAsync(
        string blobName,
        int minutes)
    {
        var blob = _azureContainer!.GetBlobClient(blobName);

        var sas = new BlobSasBuilder
        {
            BlobContainerName = _options.AzureContainerName,
            BlobName = blobName,
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(minutes)
        };
        sas.SetPermissions(BlobSasPermissions.Read);

        return Task.FromResult(blob.GenerateSasUri(sas).ToString());
    }

    private async Task<bool> CopyAzureFileAsync(
        string sourceBlob,
        string destBlob)
    {
        var source = _azureContainer!.GetBlobClient(sourceBlob);
        var dest = _azureContainer.GetBlobClient(destBlob);

        await dest.StartCopyFromUriAsync(source.Uri);
        return true;
    }

    #endregion

    #region ===== AWS S3 =====

    private async Task<string> UploadToS3Async(
        IFormFile file,
        string blobName,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();

        await _s3Client!.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _s3BucketName,
                Key = blobName,
                InputStream = stream,
                ContentType = file.ContentType
            },
            ct);

        return $"https://{_s3BucketName}.s3.{_options.AwsRegion}.amazonaws.com/{blobName}";
    }

    private async Task<ChunkUploadResult> UploadChunkToS3Async(
        IFormFile chunk,
        string blobName,
        string fileId,
        int chunkNumber,
        int totalChunks)
    {
        await using var stream = chunk.OpenReadStream();

        await _s3Client!.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _s3BucketName,
                Key = blobName,
                InputStream = stream,
                ContentType = chunk.ContentType
            });

        return new ChunkUploadResult
        {
            Success = true,
            FileId = fileId,
            ChunkNumber = chunkNumber,
            TotalChunks = totalChunks,
            UploadedBytes = chunk.Length
        };
    }

    private Task<string> CompleteS3ChunkedUploadAsync(
        string fileId,
        string finalBlobName)
    {
        return Task.FromResult(
            $"https://{_s3BucketName}.s3.{_options.AwsRegion}.amazonaws.com/{finalBlobName}");
    }

    private async Task<bool> DeleteFromS3Async(string blobName)
    {
        var res = await _s3Client!.DeleteObjectAsync(
            _s3BucketName, blobName);

        return res.HttpStatusCode == HttpStatusCode.NoContent;
    }

    private async Task<bool> CheckS3FileExistsAsync(string blobName)
    {
        try
        {
            await _s3Client!.GetObjectMetadataAsync(
                _s3BucketName, blobName);
            return true;
        }
        catch (AmazonS3Exception ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<FileMetadata?> GetS3FileMetadataAsync(
        string blobName)
    {
        var meta = await _s3Client!.GetObjectMetadataAsync(
            _s3BucketName, blobName);

        return new FileMetadata
        {
            Path = $"https://{_s3BucketName}.s3.{_options.AwsRegion}.amazonaws.com/{blobName}",
            FileName = Path.GetFileName(blobName),
            ContentType = meta.Headers.ContentType,
            SizeBytes = meta.ContentLength,
            CreatedAt = meta.LastModified?.ToUniversalTime() ?? DateTime.UtcNow,
            ModifiedAt = meta.LastModified?.ToUniversalTime() ?? DateTime.UtcNow
        };
    }

    private async Task<Stream> GetS3FileStreamAsync(string blobName)
    {
        var res = await _s3Client!.GetObjectAsync(
            _s3BucketName, blobName);

        return res.ResponseStream;
    }

    private Task<string> GenerateS3PresignedUrlAsync(
        string blobName,
        int minutes)
    {
        var url = _s3Client!.GetPreSignedURL(
            new GetPreSignedUrlRequest
            {
                BucketName = _s3BucketName,
                Key = blobName,
                Expires = DateTime.UtcNow.AddMinutes(minutes)
            });

        return Task.FromResult(url);
    }

    private async Task<bool> CopyS3FileAsync(
        string sourceBlob,
        string destBlob)
    {
        var res = await _s3Client!.CopyObjectAsync(
            new CopyObjectRequest
            {
                SourceBucket = _s3BucketName,
                SourceKey = sourceBlob,
                DestinationBucket = _s3BucketName,
                DestinationKey = destBlob
            });

        return res.HttpStatusCode == HttpStatusCode.OK;
    }

    #endregion

    #region ===== Helpers =====

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Invalid file");
    }

    private string GenerateUniqueFileName(string original)
        => $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{Path.GetExtension(original)}";

    private string BuildBlobName(string? folder, string file)
        => string.IsNullOrEmpty(folder)
            ? file
            : $"{folder}/{file}";

    private string ExtractBlobName(string path)
        => new Uri(path, UriKind.RelativeOrAbsolute).IsAbsoluteUri
            ? new Uri(path).AbsolutePath.TrimStart('/')
            : path;

    #endregion
}
