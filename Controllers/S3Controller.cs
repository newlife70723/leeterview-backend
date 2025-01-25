using Microsoft.AspNetCore.Mvc;
using LeeterviewBackend.Services;

namespace LeeterviewBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class S3Controller : ControllerBase
    {
        private readonly S3Service _s3Service;

        public S3Controller(S3Service s3Service)
        {
            _s3Service = s3Service;
        }

        [HttpGet("GenerateUrl")]
        public IActionResult GenerateUrl(string fileName, string contentType)
        {
            try
            {
                var sanitizedFileName = fileName.TrimStart('/');
                var preSignedUrl = _s3Service.GeneratePreSignedUrl(sanitizedFileName, 15, contentType); // 簽名有效期 15 分鐘
                return Ok(new { Url = preSignedUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "生成簽名 URL 發生錯誤", Error = ex.Message });
            }
        }
    }
}
