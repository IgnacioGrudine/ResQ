namespace ResQ.API.Services.Storage;

public interface IImageStorageService
{
    /// <summary>
    /// Uploads a file to MinIO under the given folder and returns the public URL.
    /// </summary>
    Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default);
}
