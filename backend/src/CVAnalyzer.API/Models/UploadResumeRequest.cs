using Microsoft.AspNetCore.Mvc;

namespace CVAnalyzer.API.Models;

public class UploadResumeRequest
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }
    
    [FromForm(Name = "userId")]
    public string? UserId { get; set; }
}
