using Microsoft.AspNetCore.Mvc;
using Shop_ProjForWeb.Core.Application.Configuration;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Shop_ProjForWeb.Core.Application.Services;
using Microsoft.Extensions.Options;

namespace Shop_ProjForWeb.Presentation.Controllers;

/// <summary>
/// Advanced file upload and management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ProductImageService _productImageService;
    private readonly FileUploadOptions _options;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileStorageService fileStorageService,
        ProductImageService productImageService,
        IOptions<FileUploadOptions> options,
        ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _productImageService = productImageService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a single file
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string? folder = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            var filePath = await _fileStorageService.UploadFileAsync(file, folder);
            var metadata = await _fileStorageService.GetFileMetadataAsync(filePath);

            return Ok(new FileUploadResponse
            {
                Success = true,
                FilePath = filePath,
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                Width = metadata?.Width,
                Height = metadata?.Height,
                UploadedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed");
            return StatusCode(500, new { error = "File upload failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Uploads multiple files
    /// </summary>
    [HttpPost("upload/multiple")]
    [ProducesResponseType(typeof(FileUploadResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMultipleFiles(IFormFileCollection files, [FromQuery] string? folder = null)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { error = "No files provided" });
        }

        if (files.Count > 10)
        {
            return BadRequest(new { error = "Maximum 10 files allowed per request" });
        }

        var results = new List<FileUploadResponse>();
        
        foreach (var file in files)
        {
            try
            {
                var filePath = await _fileStorageService.UploadFileAsync(file, folder);
                var metadata = await _fileStorageService.GetFileMetadataAsync(filePath);

                results.Add(new FileUploadResponse
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    SizeBytes = file.Length,
                    Width = metadata?.Width,
                    Height = metadata?.Height,
                    UploadedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                results.Add(new FileUploadResponse
                {
                    Success = false,
                    FileName = file.FileName,
                    ErrorMessage = ex.Message
                });
            }
        }

        return Ok(results);
    }

    /// <summary>
    /// Uploads a chunk of a file for large file support
    /// </summary>
    [HttpPost("upload/chunk")]
    [ProducesResponseType(typeof(ChunkUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadChunk(
        IFormFile chunk,
        [FromQuery] string fileId,
        [FromQuery] int chunkNumber,
        [FromQuery] int totalChunks,
        [FromQuery] string? folder = null)
    {
        if (chunk == null || chunk.Length == 0)
        {
            return BadRequest(new { error = "No chunk provided" });
        }

        if (string.IsNullOrEmpty(fileId))
        {
            return BadRequest(new { error = "File ID is required" });
        }

        if (chunkNumber < 0 || totalChunks <= chunkNumber)
        {
            return BadRequest(new { error = "Invalid chunk numbers" });
        }

        var result = await _fileStorageService.UploadChunkAsync(chunk, fileId, chunkNumber, totalChunks, folder);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// Completes a chunked upload
    /// </summary>
    [HttpPost("upload/chunk/complete")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteChunkedUpload(
        [FromQuery] string fileId,
        [FromQuery] string fileName,
        [FromQuery] string? folder = null)
    {
        if (string.IsNullOrEmpty(fileId))
        {
            return BadRequest(new { error = "File ID is required" });
        }

        if (string.IsNullOrEmpty(fileName))
        {
            return BadRequest(new { error = "File name is required" });
        }

        try
        {
            var filePath = await _fileStorageService.CompleteChunkedUploadAsync(fileId, fileName, folder);

            return Ok(new FileUploadResponse
            {
                Success = true,
                FilePath = filePath,
                FileName = fileName,
                UploadedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Uploads a product image with automatic processing
    /// </summary>
    [HttpPost("upload/product/{productId:guid}")]
    [ProducesResponseType(typeof(ProductImageUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadProductImage(IFormFile image, Guid productId)
    {
        try
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { error = "No image provided" });
            }

            var result = await _productImageService.UploadProductImageAsync(image, productId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a file
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile([FromQuery] string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest(new { error = "File path is required" });
        }

        var success = await _fileStorageService.DeleteFileAsync(filePath);

        if (!success)
        {
            return NotFound(new { error = "File not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Gets file metadata
    /// </summary>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(FileMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileMetadata([FromQuery] string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest(new { error = "File path is required" });
        }

        var metadata = await _fileStorageService.GetFileMetadataAsync(filePath);

        if (metadata == null)
        {
            return NotFound(new { error = "File not found" });
        }

        return Ok(metadata);
    }

    /// <summary>
    /// Generates a pre-signed URL for secure temporary access
    /// </summary>
    [HttpGet("presigned-url")]
    [ProducesResponseType(typeof(PresignedUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPresignedUrl(
        [FromQuery] string filePath,
        [FromQuery] int expirationMinutes = 60)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest(new { error = "File path is required" });
        }

        if (expirationMinutes < 1 || expirationMinutes > 1440) // Max 24 hours
        {
            return BadRequest(new { error = "Expiration must be between 1 and 1440 minutes" });
        }

        try
        {
            var url = await _fileStorageService.GeneratePresignedUrlAsync(filePath, expirationMinutes);

            return Ok(new PresignedUrlResponse
            {
                Url = url,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Downloads a file
    /// </summary>
    [HttpGet("download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile([FromQuery] string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest(new { error = "File path is required" });
        }

        try
        {
            var stream = await _fileStorageService.GetFileStreamAsync(filePath);
            var metadata = await _fileStorageService.GetFileMetadataAsync(filePath);

            if (metadata == null)
            {
                return NotFound(new { error = "File not found" });
            }

            return File(stream, metadata.ContentType, metadata.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "File not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Download failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets optimized image URL based on size
    /// </summary>
    [HttpGet("optimize")]
    [ProducesResponseType(typeof(OptimizedImageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOptimizedImage(
        [FromQuery] string filePath,
        [FromQuery] ImageSize size = ImageSize.Original)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest(new { error = "File path is required" });
        }

        var optimizedUrl = await _productImageService.GetOptimizedImageUrlAsync(filePath, size);

        return Ok(new OptimizedImageResponse
        {
            OriginalUrl = filePath,
            OptimizedUrl = optimizedUrl,
            Size = size
        });
    }

    /// <summary>
    /// Health check for file storage
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            Status = "Healthy",
            StorageProvider = _options.StorageProvider.ToString(),
            MaxFileSizeMb = _options.MaxFileSizeBytes / (1024 * 1024),
            AllowedImageFormats = _options.AllowedImageExtensions
        });
    }
}

/// <summary>
/// File upload response
/// </summary>
public class FileUploadResponse
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Presigned URL response
/// </summary>
public class PresignedUrlResponse
{
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Optimized image response
/// </summary>
public class OptimizedImageResponse
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string OptimizedUrl { get; set; } = string.Empty;
    public ImageSize Size { get; set; }
}
