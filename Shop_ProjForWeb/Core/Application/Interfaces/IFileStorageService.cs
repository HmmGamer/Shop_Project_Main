namespace Shop_ProjForWeb.Core.Application.Interfaces;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns the file path/URL
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="folder">Optional subfolder</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File path or URL</returns>
    Task<string> UploadFileAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a chunk of a file for chunked upload
    /// </summary>
    /// <param name="chunk">The chunk data</param>
    /// <param name="fileId">Unique file identifier</param>
    /// <param name="chunkNumber">Chunk number</param>
    /// <param name="totalChunks">Total number of chunks</param>
    /// <param name="folder">Optional subfolder</param>
    /// <returns>Chunk upload result</returns>
    Task<ChunkUploadResult> UploadChunkAsync(IFormFile chunk, string fileId, int chunkNumber, int totalChunks, string? folder = null);

    /// <summary>
    /// Completes a chunked upload and returns the final file path
    /// </summary>
    /// <param name="fileId">Unique file identifier</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="folder">Optional subfolder</param>
    /// <returns>Final file path or URL</returns>
    Task<string> CompleteChunkedUploadAsync(string fileId, string fileName, string? folder = null);

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="filePath">File path or URL</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteFileAsync(string filePath);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="filePath">File path or URL</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string filePath);

    /// <summary>
    /// Gets file metadata
    /// </summary>
    /// <param name="filePath">File path or URL</param>
    /// <returns>File metadata</returns>
    Task<FileMetadata?> GetFileMetadataAsync(string filePath);

    /// <summary>
    /// Gets a file as a stream
    /// </summary>
    /// <param name="filePath">File path or URL</param>
    /// <returns>File stream</returns>
    Task<Stream> GetFileStreamAsync(string filePath);

    /// <summary>
    /// Generates a pre-signed URL for temporary access
    /// </summary>
    /// <param name="filePath">File path or URL</param>
    /// <param name="expirationMinutes">URL expiration time in minutes</param>
    /// <returns>Pre-signed URL</returns>
    Task<string> GeneratePresignedUrlAsync(string filePath, int expirationMinutes = 60);

    /// <summary>
    /// Copies a file to a new location
    /// </summary>
    /// <param name="sourcePath">Source file path</param>
    /// <param name="destinationPath">Destination file path</param>
    /// <returns>True if copied successfully</returns>
    Task<bool> CopyFileAsync(string sourcePath, string destinationPath);
}

/// <summary>
/// Result of chunk upload
/// </summary>
public class ChunkUploadResult
{
    public bool Success { get; set; }
    public string FileId { get; set; } = string.Empty;
    public int ChunkNumber { get; set; }
    public int TotalChunks { get; set; }
    public long UploadedBytes { get; set; }
    public int UploadedPercentage => TotalChunks > 0 ? (int)((UploadedBytes / (double)TotalChunks) * 100) : 0;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// File metadata
/// </summary>
public class FileMetadata
{
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? Md5Hash { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}
