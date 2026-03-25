using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.ERP_Portal_RC.Controllers
{
    [ApiController]
    [Route("files")]
    [AllowAnonymous]
    public class FileController : ControllerBase
    {
        private readonly string _basePath;

        public FileController(IConfiguration configuration)
        {
            _basePath = configuration["FileConfig:PhysicalRootPath"] ?? @"D:\Attachments";
        }

        [HttpGet("{**filePath}")]
        public IActionResult GetFile(string filePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_basePath, filePath));

            if (!fullPath.StartsWith(Path.GetFullPath(_basePath)))
                return BadRequest();

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var contentType = Path.GetExtension(fullPath).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".xslt" or ".xml" => "application/xml",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };

            return PhysicalFile(fullPath, contentType);
        }
    }
}
