using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using ResQ.API.Models.Settings;

namespace ResQ.API.Services.Storage;

public class ImageStorageService(IMinioClient minio, IOptions<MinioSettings> options) : IImageStorageService
{
    private static readonly HashSet<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private readonly MinioSettings _settings = options.Value;

    public async Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Solo se permiten imágenes JPG, PNG o WebP.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("La imagen no puede superar los 5 MB.");

        await EnsureBucketAsync(ct);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var objectKey = $"{folder}/{Guid.NewGuid()}{extension}";

        using var stream = file.OpenReadStream();

        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType), ct);

        return $"{_settings.PublicBaseUrl}/{_settings.BucketName}/{objectKey}";
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        var exists = await minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_settings.BucketName), ct);

        if (exists) return;

        await minio.MakeBucketAsync(
            new MakeBucketArgs().WithBucket(_settings.BucketName), ct);

        // Política de lectura pública para que las URLs sean accesibles sin autenticación
        var policy = $$"""
            {
                "Version": "2012-10-17",
                "Statement": [{
                    "Effect": "Allow",
                    "Principal": {"AWS": ["*"]},
                    "Action": ["s3:GetObject"],
                    "Resource": ["arn:aws:s3:::{{_settings.BucketName}}/*"]
                }]
            }
            """;

        await minio.SetPolicyAsync(new SetPolicyArgs()
            .WithBucket(_settings.BucketName)
            .WithPolicy(policy), ct);
    }
}
