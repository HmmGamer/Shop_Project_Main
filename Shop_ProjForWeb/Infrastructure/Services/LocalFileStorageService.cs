using Shop_ProjForWeb.Core.Application.Configuration;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Shop_ProjForWeb.Infrastructure.Services;

/// <summary>
/// Local file storage service with advanced features
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly FileUploadOptions _options;
    private readonly string _basePath;
    private readonly Dictionary<string, List<string>> _chunkedFiles = new();
    private readonly SemaphoreSlim _semaphore;

    public LocalFileStorageService(IOptions<FileUploadOptions> options)
    {
        _options = options.Value;
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), _options.StorageBasePath);
        _semaphore = new SemaphoreSlim(_options.MaxConcurrentUploads, _options.MaxConcurrentUploads);
        
        EnsureDirectoryExists(_basePath);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            ValidateFile(file);
            
            var fileName = GenerateUniqueFileName(file.FileName);
            var folderPath = folder ?? GetDefaultFolder(file.ContentType);
            var fullPath = Path.Combine(_basePath, folderPath);
            
            EnsureDirectoryExists(fullPath);
            
            var filePath = Path.Combine(fullPath, fileName);
            
            // Process image if applicable
            if (IsImage(file))
            {
                await ProcessAndSaveImageAsync(file, filePath);
            }
            else
            {
                await using var stream = file.OpenReadStream();
                await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            return Path.Combine(folderPath, fileName);
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
            
            var tempPath = Path.Combine(_basePath, "temp", fileId);
            EnsureDirectoryExists(tempPath);
            
            var chunkFileName = $"chunk_{chunkNumber:D4}";
            var chunkPath = Path.Combine(tempPath, chunkFileName);
            
            await using (var stream = chunk.OpenReadStream())
            await using (var fileStream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream);
            }

            if (!_chunkedFiles.ContainsKey(fileId))
            {
                _chunkedFiles[fileId] = new List<string>();
            }
            _chunkedFiles[fileId].Add(chunkPath);

            var uploadedBytes = GetDirectorySize(tempPath);

            return new ChunkUploadResult
            {
                Success = true,
                FileId = fileId,
                ChunkNumber = chunkNumber,
                TotalChunks = totalChunks,
                UploadedBytes = uploadedBytes
            };
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
        if (!_chunkedFiles.TryGetValue(fileId, out var chunks))
        {
            throw new InvalidOperationException($"No chunks found for file ID: {fileId}");
        }

        var extension = Path.GetExtension(fileName);
        var finalFileName = GenerateUniqueFileName(fileName);
        var folderPath = folder ?? "uploads";
        var fullPath = Path.Combine(_basePath, folderPath);
        
        EnsureDirectoryExists(fullPath);
        var finalPath = Path.Combine(fullPath, finalFileName);

        // Merge chunks
        await using var finalStream = new FileStream(finalPath, FileMode.Create);
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunkPath = chunks[i];
            await using var chunkStream = new FileStream(chunkPath, FileMode.Open);
            await chunkStream.CopyToAsync(finalStream);
            
            // Delete chunk file after merging
            File.Delete(chunkPath);
        }

        // Delete temp directory
        var tempPath = Path.Combine(_basePath, "temp", fileId);
        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, true);
        }
        
        _chunkedFiles.Remove(fileId);

        return Path.Combine(folderPath, finalFileName);
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                
                // Also delete thumbnail if exists
                var thumbnailPath = Path.Combine(_basePath, "thumbnails", Path.GetFileName(filePath));
                if (File.Exists(thumbnailPath))
                {
                    File.Delete(thumbnailPath);
                }
                
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            var fileInfo = new FileInfo(fullPath);
            var metadata = new FileMetadata
            {
                Path = filePath,
                FileName = fileInfo.Name,
                ContentType = GetContentType(fileInfo.Extension),
                SizeBytes = fileInfo.Length,
                CreatedAt = fileInfo.CreationTimeUtc,
                ModifiedAt = fileInfo.LastWriteTimeUtc,
                Md5Hash = await CalculateMd5Async(fullPath)
            };

            // Get image dimensions if applicable
            if (IsImageExtension(fileInfo.Extension))
            {
                try
                {
                    using var image = await Image.LoadAsync(fullPath);
                    metadata.Width = image.Width;
                    metadata.Height = image.Height;
                }
                catch { }
            }

            return metadata;
        }
        catch
        {
            return null;
        }
    }

    public async Task<Stream> GetFileStreamAsync(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
        return await Task.FromResult(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public Task<string> GeneratePresignedUrlAsync(string filePath, int expirationMinutes = 60)
    {
        // For local storage, return a simple URL
        var baseUrl = "http://localhost:5000";
        var fullUrl = $"{baseUrl}/api/files/download?path={Uri.EscapeDataString(filePath)}";
        return Task.FromResult(fullUrl);
    }

    public Task<bool> CopyFileAsync(string sourcePath, string destinationPath)
    {
        try
        {
            var fullSourcePath = Path.Combine(_basePath, sourcePath);
            var fullDestPath = Path.Combine(_basePath, destinationPath);
            
            EnsureDirectoryExists(Path.GetDirectoryName(fullDestPath) ?? _basePath);
            
            File.Copy(fullSourcePath, fullDestPath, true);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #region Private Methods

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
        if (string.IsNullOrEmpty(extension))
        {
            throw new ArgumentException("File must have an extension");
        }

        var allowedExtensions = GetAllowedExtensions();
        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File type {extension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
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

    private string GetDefaultFolder(string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "images";
        }
        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return "videos";
        }
        if (contentType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "documents";
        }
        return "uploads";
    }

    private bool IsImage(IFormFile file)
    {
        return file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsImageExtension(string extension)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        return imageExtensions.Contains(extension.ToLowerInvariant());
    }

    private async Task ProcessAndSaveImageAsync(IFormFile file, string filePath)
    {
        using var image = await Image.LoadAsync(file.OpenReadStream());
        
        // Resize if needed
        if (_options.MaxImageWidth > 0 || _options.MaxImageHeight > 0)
        {
            var (width, height) = CalculateResizeDimensions(image.Width, image.Height);
            if (width != image.Width || height != image.Height)
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Stretch
                }));
                
                var encoder = GetImageEncoder(filePath);
                await image.SaveAsync(filePath, encoder);
                
                // Generate thumbnail
                if (_options.GenerateThumbnails)
                {
                    await GenerateThumbnailAsync(image, filePath);
                }
                
                return;
            }
        }

        // Save original
        var encoderOriginal = GetImageEncoder(filePath);
        await image.SaveAsync(filePath, encoderOriginal);

        // Generate thumbnail
        if (_options.GenerateThumbnails)
        {
            await GenerateThumbnailAsync(image, filePath);
        }
    }

    private (int width, int height) CalculateResizeDimensions(int originalWidth, int originalHeight)
    {
        var maxWidth = _options.MaxImageWidth > 0 ? _options.MaxImageWidth : originalWidth;
        var maxHeight = _options.MaxImageHeight > 0 ? _options.MaxImageHeight : originalHeight;

        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            return (originalWidth, originalHeight);
        }

        var ratio = Math.Min((double)maxWidth / originalWidth, (double)maxHeight / originalHeight);
        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }

    private async Task GenerateThumbnailAsync(Image originalImage, string originalPath)
    {
        try
        {
            var thumbnailPath = Path.Combine(_basePath, "thumbnails", Path.GetFileName(originalPath));
            EnsureDirectoryExists(Path.GetDirectoryName(thumbnailPath) ?? _basePath);

            var thumbnailWidth = _options.ThumbnailWidth;
            var thumbnailHeight = _options.ThumbnailHeight;
            
            var ratio = Math.Min((double)thumbnailWidth / originalImage.Width, (double)thumbnailHeight / originalImage.Height);
            var newWidth = (int)(originalImage.Width * ratio);
            var newHeight = (int)(originalImage.Height * ratio);

            using var thumbnail = originalImage.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(newWidth, newHeight),
                Mode = ResizeMode.Stretch
            }));

            var encoder = GetImageEncoder(originalPath);
            await thumbnail.SaveAsync(thumbnailPath, encoder);
        }
        catch (Exception ex)
        {
            // Log thumbnail generation failure but don't throw
            Console.WriteLine($"Failed to generate thumbnail: {ex.Message}");
        }
    }

    private IImageEncoder GetImageEncoder(string filePath)
    {
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return extension switch
        {
            ".png" => new PngEncoder(),
            ".webp" => new WebpEncoder(),
            _ => new JpegEncoder { Quality = _options.ImageQuality }
        };
    }

    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }

    private async Task<string> CalculateMd5Async(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await md5.ComputeHashAsync(stream);
        return Convert.ToHexString(hash);
    }

    private long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
        {
            return 0;
        }

        return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length);
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    #endregion
}
