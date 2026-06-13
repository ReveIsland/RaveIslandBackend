namespace RaveIsland.ApiService.Infrastructure.Media;

public interface IMediaStorageService
{
    Task<MediaUploadResult> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageUrl, CancellationToken cancellationToken = default);
}

public sealed record MediaUploadResult(string StorageUrl, string? ThumbnailUrl, string FileName);

public sealed class LocalMediaStorageService(IWebHostEnvironment environment) : IMediaStorageService
{
    public async Task<MediaUploadResult> UploadAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var uploadsRoot = Path.Combine(environment.ContentRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadsRoot);

        var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsRoot, safeName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var storageUrl = $"/uploads/{folder}/{safeName}";
        return new MediaUploadResult(storageUrl, null, file.FileName);
    }

    public Task DeleteAsync(string storageUrl, CancellationToken cancellationToken = default)
    {
        if (storageUrl.StartsWith("/uploads/", StringComparison.Ordinal))
        {
            var filePath = Path.Combine(environment.ContentRootPath, storageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        return Task.CompletedTask;
    }
}
