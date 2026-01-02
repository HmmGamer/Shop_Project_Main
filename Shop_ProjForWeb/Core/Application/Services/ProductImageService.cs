namespace Shop_ProjForWeb.Core.Application.Services;

using Microsoft.Extensions.Options;
using Shop_ProjForWeb.Core.Application.Configuration;

public class ProductImageService
{
    private readonly FileUploadOptions _options;
    private readonly string _basePath;

    public ProductImageService(IOptions<FileUploadOptions> options)
    {
        _options = options.Value;
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), _options.UploadFolder);
    }

    public async Task<string> UploadImageAsync(IFormFile file, Guid productId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {_options.MaxFileSizeBytes} bytes");
        }

        // Ensure upload directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }

        // Generate unique filename
        var fileName = $"{productId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(_basePath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative path for storage in database
        return Path.Combine(_options.UploadFolder, fileName).Replace("\\", "/");
    }

    public void DeleteImage(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            return;
        }

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), imagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
