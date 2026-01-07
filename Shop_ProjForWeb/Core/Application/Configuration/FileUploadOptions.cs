namespace Shop_ProjForWeb.Core.Application.Configuration;

/// <summary>
/// Storage provider types
/// </summary>
public enum StorageProvider
{
    Local = 0,
    AzureBlob = 1,
    AwsS3 = 2
}

public class FileUploadOptions
{
    public long MaxFileSizeBytes { get; set; } = 5242880; // 5MB default
    public string UploadFolder { get; set; } = "UploadedFiles";
    
    // Storage settings
    public string StorageBasePath { get; set; } = "wwwroot/uploads";
    public int MaxConcurrentUploads { get; set; } = 10;
    
    // Allowed extensions
    public string AllowedImageExtensions { get; set; } = ".jpg,.jpeg,.png,.gif,.webp";
    public string AllowedDocumentExtensions { get; set; } = ".pdf,.doc,.docx,.xls,.xlsx";
    public string AllowedVideoExtensions { get; set; } = ".mp4,.avi,.mkv,.mov,.webm";
    
    // Image processing settings
    public int MaxImageWidth { get; set; } = 1920;
    public int MaxImageHeight { get; set; } = 1080;
    public int ThumbnailWidth { get; set; } = 200;
    public int ThumbnailHeight { get; set; } = 200;
    public int ImageQuality { get; set; } = 85;
    public bool GenerateThumbnails { get; set; } = true;
    
    // Cloud storage settings
    public StorageProvider StorageProvider { get; set; } = StorageProvider.Local; // local, aws, azure
    public string AwsAccessKey { get; set; } = "";
    public string AwsSecretKey { get; set; } = "";
    public string AwsRegion { get; set; } = "";
    public string AwsS3BucketName { get; set; } = "";
    public string AzureStorageConnectionString { get; set; } = "";
    public string AzureContainerName { get; set; } = "";
}
