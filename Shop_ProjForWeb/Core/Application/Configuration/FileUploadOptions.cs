namespace Shop_ProjForWeb.Core.Application.Configuration;

public class FileUploadOptions
{
    public long MaxFileSizeBytes { get; set; } = 5242880; // 5MB default
    public string UploadFolder { get; set; } = "UploadedFiles";
}
