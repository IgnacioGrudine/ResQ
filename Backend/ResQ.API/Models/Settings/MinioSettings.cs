namespace ResQ.API.Models.Settings;

/// <summary>
/// Strongly-typed configuration for the MinIO S3-compatible object storage service
/// used to store merchant profile photos and product images.
/// Bound from the <c>MinioSettings</c> section in <c>appsettings.json</c>
/// (or the corresponding environment variable overrides in production).
/// </summary>
public class MinioSettings
{
    /// <summary>
    /// Host and port of the MinIO server (e.g. <c>localhost:9000</c>).
    /// In Docker Compose this points to the MinIO container. Sourced from <c>MinioSettings:Endpoint</c>.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Access key (username) used to authenticate with the MinIO server.
    /// Equivalent to AWS <c>AccessKeyId</c>. Sourced from <c>MinioSettings:AccessKey</c>.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret key (password) used together with <see cref="AccessKey"/> to authenticate.
    /// Equivalent to AWS <c>SecretAccessKey</c>. Sourced from <c>MinioSettings:SecretKey</c>.
    /// Never store this value in source control.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Name of the MinIO bucket where all platform assets are stored.
    /// The bucket must exist and be accessible with the configured credentials.
    /// Sourced from <c>MinioSettings:BucketName</c>.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Specifies whether the connection to MinIO should use HTTPS.
    /// Typically <c>false</c> in local development (plain HTTP) and <c>true</c> in production.
    /// Sourced from <c>MinioSettings:UseSSL</c>.
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// The publicly accessible base URL prepended to object keys to produce the full
    /// URL returned in API responses (e.g. <c>https://storage.resq.com.ar</c>).
    /// May differ from <see cref="Endpoint"/> when a CDN or reverse proxy is in front of MinIO.
    /// Sourced from <c>MinioSettings:PublicBaseUrl</c>.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
