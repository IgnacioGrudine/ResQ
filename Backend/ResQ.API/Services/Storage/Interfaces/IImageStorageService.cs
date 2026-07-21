namespace ResQ.API.Services.Storage;

public interface IImageStorageService
{
    /// <summary>
    /// Uploads a file to MinIO under the given folder and returns its storage-relative path.
    /// </summary>
    /// <param name="file">The image file received from the HTTP multipart request.</param>
    /// <param name="folder">
    /// The logical folder (object prefix) inside the MinIO bucket under which the
    /// file will be stored, e.g. <c>merchants</c> or <c>products</c>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The bucket-relative object path (e.g. <c>resq-images/products/3/{uuid}.jpg</c>) to persist
    /// in the database. This is deliberately not a full URL — resolve it to a public URL with
    /// <see cref="ResolvePublicUrl"/> only when building an API response, so the configured host
    /// (local, ngrok, or a future production domain) is always applied fresh rather than baked in
    /// permanently at upload time.
    /// </returns>
    Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default);

    /// <summary>
    /// Resolves a stored image path into a publicly accessible URL using the current
    /// <c>Minio:PublicBaseUrl</c> configuration.
    /// </summary>
    /// <param name="storedValue">
    /// The value persisted on the entity — either a relative path produced by <see cref="UploadAsync"/>,
    /// or a legacy/seeded absolute URL (e.g. an external image link), which is returned unchanged.
    /// </param>
    /// <returns>A publicly accessible URL, or the original value if it is null, empty, or already absolute.</returns>
    string? ResolvePublicUrl(string? storedValue);
}
