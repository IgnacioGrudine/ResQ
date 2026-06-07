namespace ResQ.API.Services.Storage;

public interface IImageStorageService
{
    /// <summary>
    /// Uploads a file to MinIO under the given folder and returns the public URL.
    /// </summary>
    /// <param name="file">The image file received from the HTTP multipart request.</param>
    /// <param name="folder">
    /// The logical folder (object prefix) inside the MinIO bucket under which the
    /// file will be stored, e.g. <c>merchants</c> or <c>products</c>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The publicly accessible URL of the uploaded object that can be stored in the
    /// database and returned to clients.
    /// </returns>
    Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default);
}
