namespace RaveIsland.ApiService.Infrastructure.Media;

public interface IMediaStorageService
{
    Task<MediaUploadResult> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageUrl, CancellationToken cancellationToken = default);
}

public sealed record MediaUploadResult(string StorageUrl, string? ThumbnailUrl, string FileName);

public sealed class LocalMediaStorageService(IWebHostEnvironment environment, ILogger<LocalMediaStorageService> logger) : IMediaStorageService
{
    public async Task<MediaUploadResult> UploadAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var uploadsRoot = Path.Combine(environment.ContentRootPath, "uploads", folder.Replace('\\', '/').Trim('/'));
        Directory.CreateDirectory(uploadsRoot);

        var originalName = SanitizeFileName(file.FileName);
        var extension = Path.GetExtension(originalName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = GuessExtension(file.ContentType);
        }

        var safeName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsRoot, safeName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        logger.LogInformation("Stored upload at {FilePath}", filePath);

        var normalizedFolder = folder.Replace('\\', '/').Trim('/');
        var storageUrl = $"/uploads/{normalizedFolder}/{safeName}";
        return new MediaUploadResult(storageUrl, null, originalName);
    }

    public Task DeleteAsync(string storageUrl, CancellationToken cancellationToken = default)
    {
        if (storageUrl.StartsWith("/uploads/", StringComparison.Ordinal))
        {
            var relativePath = storageUrl["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(environment.ContentRootPath, "uploads", relativePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        return Task.CompletedTask;
    }

    private static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "upload.bin";
        }

        var trimmed = Path.GetFileName(fileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "upload.bin";
        }

        return trimmed.Length <= 512 ? trimmed : trimmed[..512];
    }

    private static string GuessExtension(string? contentType) =>
        contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".bin",
        };
}
