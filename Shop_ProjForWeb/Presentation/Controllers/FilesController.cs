namespace Shop_ProjForWeb.Presentation.Controllers;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Handles file upload and download operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

    public FilesController()
    {
        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
        }
    }

    /// <summary>
    /// Uploads a file to the server
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <returns>Generated filename for the uploaded file</returns>
    /// <response code="200">File uploaded successfully</response>
    /// <response code="400">No file provided or file is empty</response>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(_uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { fileName });
    }

    /// <summary>
    /// Downloads a previously uploaded file
    /// </summary>
    /// <param name="fileName">The name of the file to download</param>
    /// <returns>The requested file</returns>
    /// <response code="200">File downloaded successfully</response>
    /// <response code="404">File not found</response>
    [HttpGet("download/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Download(string fileName)
    {
        var filePath = Path.Combine(_uploadFolder, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "application/octet-stream", fileName);
    }
}
