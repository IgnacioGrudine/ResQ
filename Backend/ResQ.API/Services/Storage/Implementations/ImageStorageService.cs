using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using ResQ.API.Models.Settings;

namespace ResQ.API.Services.Storage;

/// <summary>
/// Handles uploading product and merchant profile images to MinIO object storage.
/// Produces publicly accessible URLs using the configured base URL and bucket name.
/// </summary>
/// <remarks>
/// The service enforces a content-type allowlist (JPEG, PNG, WebP) and a 5 MB file-size cap
/// before any upload is attempted. On first upload, if the target bucket does not yet exist,
/// it is created and an S3-compatible public-read bucket policy is applied so that stored
/// image URLs are accessible without authentication.
/// </remarks>
public class ImageStorageService(IMinioClient minio, IOptions<MinioSettings> options) : IImageStorageService
{
    private static readonly HashSet<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private readonly MinioSettings _settings = options.Value;

    /// <summary>
    /// Validates, uploads an image file to MinIO, and returns its public URL.
    /// </summary>
    /// <param name="file">
    /// The image file received from an HTTP multipart request.
    /// Must have a content type of <c>image/jpeg</c>, <c>image/png</c>, or <c>image/webp</c>,
    /// and must not exceed 5 MB.
    /// </param>
    /// <param name="folder">
    /// The virtual folder path within the bucket (e.g., <c>products/42</c> or <c>merchants/7</c>).
    /// A new UUID-based filename is generated per upload to avoid collisions and cache-busting issues.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The full public URL of the uploaded object in the format
    /// <c>{PublicBaseUrl}/{BucketName}/{folder}/{uuid}{extension}</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the file's content type is not in the allowlist, or if the file exceeds 5 MB.
    /// </exception>
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

    /// <summary>
    /// Ensures the configured MinIO bucket exists, creating it with a public-read policy if it does not.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// This method is called before every upload. The existence check is a lightweight HEAD
    /// request and incurs minimal overhead. If the bucket already exists the method returns
    /// immediately without modifying any policies.
    ///
    /// The public-read policy grants anonymous <c>s3:GetObject</c> access to all objects in
    /// the bucket so that image URLs returned to clients are directly accessible without
    /// pre-signed tokens. Write and delete access remains restricted to the MinIO credentials
    /// configured in the application.
    /// </remarks>
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
