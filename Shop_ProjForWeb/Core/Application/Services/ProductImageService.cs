using Shop_ProjForWeb.Core.Application.Configuration;
using Shop_ProjForWeb.Core.Application.Interfaces;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Shop_ProjForWeb.Core.Application.Services;

/// <summary>
/// Enhanced service for managing product images with advanced processing capabilities
/// </summary>
public class ProductImageService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly FileUploadOptions _options;
    private readonly ILogger<ProductImageService> _logger;

    // Allowed image formats
    private static readonly string[] AllowedFormats = { "image/jpeg", "image/png", "image/gif", "image/webp" };

    public ProductImageService(
        IFileStorageService fileStorageService,
        IOptions<FileUploadOptions> options,
        ILogger<ProductImageService> logger)
    {
        _fileStorageService = fileStorageService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a product image with automatic processing
    /// </summary>
    /// <param name="image">The image file</param>
    /// <param name="productId">Product ID for organizing files</param>
    /// <returns>Upload result with image URL and thumbnail URL</returns>
    public async Task<ProductImageUploadResult> UploadProductImageAsync(IFormFile image, Guid productId)
    {
        ValidateImage(image);

        var folder = $"products/{productId}";
        
        // Upload main image
        var imagePath = await _fileStorageService.UploadFileAsync(image, folder);
        
        // Get image metadata
        var metadata = await _fileStorageService.GetFileMetadataAsync(imagePath);
        
        // Generate thumbnail path
        var thumbnailPath = await GenerateThumbnailAsync(imagePath, folder, productId);

        _logger.LogInformation("Product image uploaded successfully: {ImagePath}", imagePath);

        return new ProductImageUploadResult
        {
            ImageUrl = imagePath,
            ThumbnailUrl = thumbnailPath,
            FileName = Path.GetFileName(imagePath),
            ContentType = image.ContentType,
            SizeBytes = image.Length,
            Width = metadata?.Width,
            Height = metadata?.Height,
            UploadedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Uploads multiple product images
    /// </summary>
    /// <param name="images">Array of image files</param>
    /// <param name="productId">Product ID</param>
    /// <returns>Array of upload results</returns>
    public async Task<ProductImageUploadResult[]> UploadProductImagesAsync(IFormFile[] images, Guid productId)
    {
        var results = new List<ProductImageUploadResult>();
        var tasks = images.Select(img => UploadProductImageAsync(img, productId));
        results = (await Task.WhenAll(tasks)).ToList();
        return results.ToArray();
    }

    /// <summary>
    /// Deletes a product image and its thumbnail
    /// </summary>
    /// <param name="imagePath">Path to the image</param>
    /// <returns>True if deleted successfully</returns>
    public async Task<bool> DeleteProductImageAsync(string imagePath)
    {
        var success = await _fileStorageService.DeleteFileAsync(imagePath);
        
        if (success)
        {
            _logger.LogInformation("Product image deleted: {ImagePath}", imagePath);
        }
        
        return success;
    }

    /// <summary>
    /// Updates a product image (replaces existing)
    /// </summary>
    /// <param name="oldImagePath">Path to the old image</param>
    /// <param name="newImage">New image file</param>
    /// <param name="productId">Product ID</param>
    /// <returns>Upload result for the new image</returns>
    public async Task<ProductImageUploadResult> UpdateProductImageAsync(string oldImagePath, IFormFile newImage, Guid productId)
    {
        // Delete old image
        await DeleteProductImageAsync(oldImagePath);
        
        // Upload new image
        return await UploadProductImageAsync(newImage, productId);
    }

    /// <summary>
    /// Gets optimized image URL based on size requirement
    /// </summary>
    /// <param name="imagePath">Original image path</param>
    /// <param name="size">Desired size (original, large, medium, small, thumbnail)</param>
    /// <returns>URL for the optimized image</returns>
    public async Task<string> GetOptimizedImageUrlAsync(string imagePath, ImageSize size = ImageSize.Original)
    {
        switch (size)
        {
            case ImageSize.Original:
                return imagePath;
            
            case ImageSize.Large:
                return imagePath.Replace("/images/", "/images/large/");
            
            case ImageSize.Medium:
                return imagePath.Replace("/images/", "/images/medium/");
            
            case ImageSize.Small:
                return imagePath.Replace("/images/", "/images/small/");
            
            case ImageSize.Thumbnail:
                var thumbnailPath = await GetThumbnailPathAsync(imagePath);
                return thumbnailPath ?? imagePath;
            
            default:
                return imagePath;
        }
    }

    /// <summary>
    /// Generates a pre-signed URL for temporary secure access
    /// </summary>
    /// <param name="imagePath">Image path</param>
    /// <param name="expirationMinutes">URL expiration time</param>
    /// <returns>Pre-signed URL</returns>
    public async Task<string> GenerateSecureUrlAsync(string imagePath, int expirationMinutes = 60)
    {
        return await _fileStorageService.GeneratePresignedUrlAsync(imagePath, expirationMinutes);
    }

    /// <summary>
    /// Validates image file
    /// </summary>
    private void ValidateImage(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            throw new ArgumentException("Image file is empty or null");
        }

        if (!AllowedFormats.Contains(image.ContentType))
        {
            throw new ArgumentException($"Invalid image format. Allowed formats: {string.Join(", ", AllowedFormats)}");
        }

        var maxSizeBytes = _options.MaxFileSizeBytes;
        if (image.Length > maxSizeBytes)
        {
            throw new ArgumentException($"Image size exceeds maximum allowed size of {maxSizeBytes / (1024 * 1024)}MB");
        }

        // Validate image dimensions
        try
        {
            using var imageStream = image.OpenReadStream();
            using var img = Image.Load(imageStream);
            if (img.Width < 50 || img.Height < 50)
            {
                throw new ArgumentException("Image dimensions are too small (minimum: 50x50 pixels)");
            }
        }
        catch
        {
            throw new ArgumentException("Invalid image file");
        }
    }

    private async Task<string> GenerateThumbnailAsync(string imagePath, string folder, Guid productId)
    {
        try
        {
            var thumbnailFolder = $"thumbnails/{productId}";
            var thumbnailFileName = Path.GetFileName(imagePath);
            var thumbnailPath = Path.Combine(thumbnailFolder, thumbnailFileName);

            // Copy original image to thumbnail folder with same name
            await _fileStorageService.CopyFileAsync(imagePath, thumbnailPath);

            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to generate thumbnail: {Error}", ex.Message);
            return imagePath;
        }
    }

    private Task<string?> GetThumbnailPathAsync(string imagePath)
    {
        var thumbnailPath = imagePath.Replace("/images/", "/thumbnails/");
        return Task.FromResult<string?>(thumbnailPath);
    }

    /// <summary>
    /// Deletes an image from storage
    /// </summary>
    public void DeleteImage(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            return;
        }
        
        try
        {
            _fileStorageService.DeleteFileAsync(imagePath).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to delete image: {Error}", ex.Message);
        }
    }

    /// <summary>
    /// Uploads an image for a product
    /// </summary>
    public async Task<string> UploadImageAsync(IFormFile file, Guid productId)
    {
        var result = await UploadProductImageAsync(file, productId);
        return result.ImageUrl;
    }
}

/// <summary>
/// Result of product image upload
/// </summary>
public class ProductImageUploadResult
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// Image size options for optimization
/// </summary>
public enum ImageSize
{
    Original = 0,
    Large = 1,
    Medium = 2,
    Small = 3,
    Thumbnail = 4
}
