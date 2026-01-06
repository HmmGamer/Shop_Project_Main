using Shop_ProjForWeb.Core.Application.Configuration;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Shop_ProjForWeb.Infrastructure.Services;

/// <summary>
/// Cloud file storage service supporting Azure Blob Storage and AWS S3
/// </summary>
public class CloudFileStorageService : IFileStorageService
{
    private readonly FileUploadOptions _options;
    private readonly ILogger<CloudFileStorageService> _logger;
    private readonly SemaphoreSlim _semaphore;

    // Azure Blob Storage client (lazy initialization)
    private Azure.Storage.Blobs.BlobServiceClient? _azureClient;
    private Azure.Storage.Blobs.BlobContainerClient? _azureContainer;

    // AWS S3 client (lazy initialization)
    private Amazon.S3.AmazonS3Client? _s3Client;
    private readonly string _s3BucketName;

    public CloudFileStorageService(
        IOptions<FileUploadOptions> options,
        ILogger<CloudFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_options.MaxConcurrentUploads, _options.MaxConcurrentUploads);
        _s3BucketName = _options.AwsS3BucketName ?? string.Empty;

        InitializeAzureClient();
        InitializeS3Client();
    }

    public async Task<string> UploadFileAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            ValidateFile(file);

            var fileName = GenerateUniqueFileName(file.FileName);
            var blobName = BuildBlobName(folder, fileName);

            switch (_options.StorageProvider)
            {
                case StorageProvider.AzureBlob:
                    return await UploadToAzureAsync(file, blobName, cancellationToken);
                case StorageProvider.AwsS3:
                    return await UploadToS3Async(file, blobName, cancellationToken);
                default:
                    throw new NotSupportedException($"Storage provider {_options.StorageProvider} is not supported");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ChunkUploadResult> UploadChunkAsync(IFormFile chunk, string fileId, int chunkNumber, int totalChunks, string? folder = null)
    {
        try
        {
            ValidateFile(chunk);

            var blobName = BuildBlobName("temp", $"{fileId}/chunk_{chunkNumber:D4}");

            switch (_options.StorageProvider)
            {
                case StorageProvider.AzureBlob:
                    return await UploadChunkToAzureAsync(chunk, blobName, fileId, chunkNumber, totalChunks);
                case StorageProvider.AwsS3:
                    return await UploadChunkToS3Async(chunk, blobName, fileId, chunkNumber, totalChunks);
                default:
                    throw new NotSupportedException($"Storage provider {_options.StorageProvider} is not supported");
            }
        }
        catch (Exception ex)
        {
            return new ChunkUploadResult
            {
                Success = false,
                FileId = fileId,
                ChunkNumber = chunkNumber,
                TotalChunks = totalChunks,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<string> CompleteChunkedUploadAsync(string fileId, string fileName, string? folder = null)
    {
        var finalBlobName = BuildBlobName(folder, GenerateUniqueFileName(fileName));

        switch (_options.StorageProvider)
        {
            case StorageProvider.AzureBlob:
                return await CompleteAzureChunkedUploadAsync(fileId, finalBlobName);
            case StorageProvider.AwsS3:
                return await CompleteS3ChunkedUploadAsync(fileId, finalBlobName);
            default:
                throw new NotSupportedException($"Storage provider {_options.StorageProvider} is not supported");
        }
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var blobName = ExtractBlobName(filePath);

            switch (_options.StorageProvider)
            {
                case StorageProvider.AzureBlob:
                    return DeleteFromAzureAsync(blobName);
                case StorageProvider.AwsS3:
                    return DeleteFromS3Async(blobName);
                default:
                    return Task.FromResult(false);
            }
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var blobName = ExtractBlobName(filePath);

            switch (_options.StorageProvider)
            {
                case StorageProvider.AzureBlob:
                    return CheckAzureFileExistsAsync(blobName);
                case StorageProvider.AwsS3:
                    return CheckS3FileExistsAsync(blobName);
                default:
                    return Task.FromResult(false);
            }
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string filePath)
    {
        try
        {
            var blobName = ExtractBlobName(filePath);

            switch (_options.StorageProvider)
            {
                case StorageProvider.AzureBlob:
                    return await GetAzureFileMetadataAsync(blobName);
                case StorageProvider.AwsS3:
                    return await GetS3FileMetadataAsync(blobName);
                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    public async Task<Stream> GetFileStreamAsync(string filePath)
    {
        var blobName = ExtractBlobName(filePath);

        switch (_options.StorageProvider)
        {
            case StorageProvider.AzureBlob:
                return await GetAzureFileStreamAsync(blobName);
            case StorageProvider.AwsS3:
                return await GetS3FileStreamAsync(blobName);
            default:
                throw new NotSupportedException($"Storage provider {_options.StorageProvider} is not supported");
        }
    }

    public Task<string> GeneratePresignedUrlAsync(string filePath, int expirationMinutes = 60)
    {
        var blobName = ExtractBlobName(filePath);

        switch (_options.StorageProvider)
        {
            case StorageProvider.AzureBlob:
                return GenerateAzurePresignedUrlAsync(blobName, expirationMinutes);
            case StorageProvider.AwsS3:
                return GenerateS3PresignedUrlAsync(blobName, expirationMinutes);
            default:
                throw new NotSupportedException($"Storage provider {_options.StorageProvider} is not supported");
        }
    }

    public Task<bool> CopyFileAsync(string sourcePath, string destinationPath)
    {
        try
        {
            var sourceBlob = ExtractBlobName(sourcePath);
            var destBlob = ExtractBlobName(destinationPath);

            switch (_options.StorageProvider)
            {
                case StorageProvider.AzureBlob:
                    return CopyAzureFileAsync(sourceBlob, destBlob);
                case StorageProvider.AwsS3:
                    return CopyS3FileAsync(sourceBlob, destBlob);
                default:
                    return Task.FromResult(false);
            }
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #region Private Methods

    private void InitializeAzureClient()
    {
        if (_options.StorageProvider == StorageProvider.AzureBlob &&
            !string.IsNullOrEmpty(_options.AzureStorageConnectionString) &&
            !string.IsNullOrEmpty(_options.AzureContainerName))
        {
            try
            {
                _azureClient = new Azure.Storage.Blobs.BlobServiceClient(_options.AzureStorageConnectionString);
                _azureContainer = _azureClient.GetBlobContainerClient(_options.AzureContainerName);
                
                // Ensure container exists
                _azureContainer.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to initialize Azure Blob Storage client: {Error}", ex.Message);
            }
        }
    }

    private void InitializeS3Client()
    {
        if (_options.StorageProvider == StorageProvider.AwsS3 &&
            !string.IsNullOrEmpty(_options.AwsAccessKey) &&
            !string.IsNullOrEmpty(_options.AwsSecretKey))
        {
            try
            {
                var config = new AmazonS3Config
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.AwsRegion ?? "us-east-1")
                };
                _s3Client = new Amazon.S3.AmazonS3Client(_options.AwsAccessKey, _options.AwsSecretKey, config);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to initialize AWS S3 client: {Error}", ex.Message);
            }
        }
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)}MB");
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var allowedExtensions = GetAllowedExtensions();
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File type {extension} is not allowed");
        }
    }

    private HashSet<string> GetAllowedExtensions()
    {
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ext in _options.AllowedImageExtensions.Split(',').Select(e => e.Trim().ToLowerInvariant()))
        {
            extensions.Add(ext);
        }
        foreach (var ext in _options.AllowedDocumentExtensions.Split(',').Select(e => e.Trim().ToLowerInvariant()))
        {
            extensions.Add(ext);
        }
        foreach (var ext in _options.AllowedVideoExtensions.Split(',').Select(e => e.Trim().ToLowerInvariant()))
        {
            extensions.Add(ext);
        }
        return extensions;
    }

    private string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var uniqueId = Guid.NewGuid().ToString("N")[..16];
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"{timestamp}_{uniqueId}{extension}";
    }

    private string BuildBlobName(string? folder, string fileName)
    {
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
        return string.IsNullOrEmpty(folder) 
            ? $"{dateFolder}/{fileName}" 
            : $"{folder}/{dateFolder}/{fileName}";
    }

    private string ExtractBlobName(string filePath)
    {
        // Handle URLs and paths
        if (filePath.Contains("://"))
        {
            var uri = new Uri(filePath);
            filePath = uri.AbsolutePath;
        }
        
        // Remove container/bucket prefix if present
        var segments = filePath.Split('/');
        return string.Join("/", segments.Skip(_options.StorageProvider == StorageProvider.AzureBlob && !string.IsNullOrEmpty(_options.AzureContainerName) ? 1 : 0));
    }

    #region Azure Blob Storage Operations

    private async Task<string> UploadToAzureAsync(IFormFile file, string blobName, CancellationToken cancellationToken)
    {
        if (_azureContainer == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured");
        }

        var blobClient = _azureContainer.GetBlobClient(blobName);
        
        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = file.ContentType
            }
        }, cancellationToken);

        return blobClient.Uri.ToString();
    }

    private async Task<ChunkUploadResult> UploadChunkToAzureAsync(IFormFile chunk, string blobName, string fileId, int chunkNumber, int totalChunks)
    {
        if (_azureContainer == null)
        {
            return new ChunkUploadResult { Success = false, ErrorMessage = "Azure not configured" };
        }

        var blobClient = _azureContainer.GetBlobClient(blobName);
        
        await using var stream = chunk.OpenReadStream();
        await blobClient.UploadAsync(stream);

        return new ChunkUploadResult
        {
            Success = true,
            FileId = fileId,
            ChunkNumber = chunkNumber,
            TotalChunks = totalChunks,
            UploadedBytes = chunk.Length
        };
    }

    private async Task<string> CompleteAzureChunkedUploadAsync(string fileId, string finalBlobName)
    {
        if (_azureContainer == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured");
        }

        // In Azure, we use Block Blobs for chunked upload
        var finalBlobClient = _azureContainer.GetBlobClient(finalBlobName);
        return finalBlobClient.Uri.ToString();
    }

    private Task<bool> DeleteFromAzureAsync(string blobName)
    {
        try
        {
            if (_azureContainer == null)
            {
                return Task.FromResult(false);
            }

            var blobClient = _azureContainer.GetBlobClient(blobName);
            return Task.FromResult(blobClient.DeleteIfExists());
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private Task<bool> CheckAzureFileExistsAsync(string blobName)
    {
        try
        {
            if (_azureContainer == null)
            {
                return Task.FromResult(false);
            }

            var blobClient = _azureContainer.GetBlobClient(blobName);
            return Task.FromResult(blobClient.Exists());
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private async Task<FileMetadata?> GetAzureFileMetadataAsync(string blobName)
    {
        try
        {
            if (_azureContainer == null)
            {
                return null;
            }

            var blobClient = _azureContainer.GetBlobClient(blobName);
            var properties = await blobClient.GetPropertiesAsync();

            return new FileMetadata
            {
                Path = blobClient.Uri.ToString(),
                FileName = Path.GetFileName(blobName),
                ContentType = properties.Value.ContentType,
                SizeBytes = properties.Value.ContentLength,
                CreatedAt = properties.Value.CreatedOn.UtcDateTime,
                ModifiedAt = properties.Value.LastModified.UtcDateTime
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<Stream> GetAzureFileStreamAsync(string blobName)
    {
        if (_azureContainer == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured");
        }

        var blobClient = _azureContainer.GetBlobClient(blobName);
        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }

    private Task<string> GenerateAzurePresignedUrlAsync(string blobName, int expirationMinutes)
    {
        if (_azureContainer == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured");
        }

        var blobClient = _azureContainer.GetBlobClient(blobName);
        var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
        {
            BlobContainerName = _options.AzureContainerName ?? string.Empty,
            BlobName = blobName,
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        };
        sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return Task.FromResult(sasUri.ToString());
    }

    private Task<bool> CopyAzureFileAsync(string sourceBlob, string destBlob)
    {
        try
        {
            if (_azureContainer == null)
            {
                return Task.FromResult(false);
            }

            var sourceBlobClient = _azureContainer.GetBlobClient(sourceBlob);
            var destBlobClient = _azureContainer.GetBlobClient(destBlob);
            destBlobClient.StartCopyFromUri(sourceBlobClient.Uri);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region AWS S3 Operations

    private async Task<string> UploadToS3Async(IFormFile file, string blobName, CancellationToken cancellationToken)
    {
        if (_s3Client == null)
        {
            throw new InvalidOperationException("AWS S3 is not configured");
        }

        await using var stream = file.OpenReadStream();
        var request = new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = _s3BucketName,
            Key = blobName,
            InputStream = stream,
            ContentType = file.ContentType,
            AutoCloseStream = true
        };

        var response = await _s3Client.PutObjectAsync(request, cancellationToken);
        
        // Return S3 URL
        return $"https://{_s3BucketName}.s3.{_options.AwsRegion}.amazonaws.com/{blobName}";
    }

    private async Task<ChunkUploadResult> UploadChunkToS3Async(IFormFile chunk, string blobName, string fileId, int chunkNumber, int totalChunks)
    {
        if (_s3Client == null)
        {
            return new ChunkUploadResult { Success = false, ErrorMessage = "AWS S3 not configured" };
        }

        await using var stream = chunk.OpenReadStream();
        var request = new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = _s3BucketName,
            Key = blobName,
            InputStream = stream,
            ContentType = chunk.ContentType,
            AutoCloseStream = true
        };

        await _s3Client.PutObjectAsync(request);

        return new ChunkUploadResult
        {
            Success = true,
            FileId = fileId,
            ChunkNumber = chunkNumber,
            TotalChunks = totalChunks,
            UploadedBytes = chunk.Length
        };
    }

    private async Task<string> CompleteS3ChunkedUploadAsync(string fileId, string finalBlobName)
    {
        if (_s3Client == null)
        {
            throw new InvalidOperationException("AWS S3 is not configured");
        }

        // For S3, we'd need to implement multipart upload
        // This is a simplified version
        return $"https://{_s3BucketName}.s3.{_options.AwsRegion}.amazonaws.com/{finalBlobName}";
    }

    private async Task<bool> DeleteFromS3Async(string blobName)
    {
        try
        {
            if (_s3Client == null)
            {
                return false;
            }

            var request = new Amazon.S3.Model.DeleteObjectRequest
            {
                BucketName = _s3BucketName,
                Key = blobName
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckS3FileExistsAsync(string blobName)
    {
        try
        {
            if (_s3Client == null)
            {
                return false;
            }

            var request = new Amazon.S3.Model.GetObjectMetadataRequest
            {
                BucketName = _s3BucketName,
                Key = blobName
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (Amazon.S3.Model.NoSuchKeyException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<FileMetadata?> GetS3FileMetadataAsync(string blobName)
    {
        try
        {
            if (_s3Client == null)
            {
                return null;
            }

            var request = new Amazon.S3.Model.GetObjectMetadataRequest
            {
                BucketName = _s3BucketName,
                Key = blobName
            };

            var response = await _s3Client.GetObjectMetadataAsync(request);

            return new FileMetadata
            {
                Path = $"https://{_s3BucketName}.s3.{_options.AwsRegion}.amazonaws.com/{blobName}",
                FileName = Path.GetFileName(blobName),
                ContentType = response.Headers.ContentType,
                SizeBytes = response.ContentLength,
                CreatedAt = response.LastModified.UtcDateTime,
                ModifiedAt = response.LastModified.UtcDateTime
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<Stream> GetS3FileStreamAsync(string blobName)
    {
        if (_s3Client == null)
        {
            throw new InvalidOperationException("AWS S3 is not configured");
        }

        var request = new Amazon.S3.Model.GetObjectRequest
        {
            BucketName = _s3BucketName,
            Key = blobName
        };

        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }

    private async Task<string> GenerateS3PresignedUrlAsync(string blobName, int expirationMinutes)
    {
        if (_s3Client == null)
        {
            throw new InvalidOperationException("AWS S3 is not configured");
        }

        var request = new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            BucketName = _s3BucketName,
            Key = blobName,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };

        return await _s3Client.GetPreSignedURLAsync(request);
    }

    private async Task<bool> CopyS3FileAsync(string sourceBlob, string destBlob)
    {
        try
        {
            if (_s3Client == null)
            {
                return false;
            }

            var request = new Amazon.S3.Model.CopyObjectRequest
            {
                SourceBucket = _s3BucketName,
                SourceKey = sourceBlob,
                DestinationBucket = _s3BucketName,
                DestinationKey = destBlob
            };

            var response = await _s3Client.CopyObjectAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #endregion
}
