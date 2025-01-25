using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace LeeterviewBackend.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Service(IConfiguration configuration)
        {
            var awsOptions = configuration.GetSection("AWS");
            _bucketName = "leeterview-bucket"; // 替換為你的 S3 存放桶名稱

            // 讀取 AWS 配置
            var accessKey = awsOptions["AccessKey"];
            var secretKey = awsOptions["SecretKey"];
            var region = awsOptions["Region"];

            // 初始化 S3 客戶端
            _s3Client = new AmazonS3Client(
                accessKey,
                secretKey,
                RegionEndpoint.GetBySystemName(region)
            );
        }

        public string GeneratePreSignedUrl(string objectKey, int durationInMinutes, string contentType)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.AddMinutes(durationInMinutes),
                Verb = HttpVerb.PUT,
                Headers =
                {
                    CacheControl = "public, max-age=3240000",
                    ["Content-Type"] = contentType
                }
            };

            return _s3Client.GetPreSignedURL(request);
        }
    }
}
