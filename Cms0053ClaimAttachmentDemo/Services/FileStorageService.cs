namespace Cms0053ClaimAttachmentDemo.Services;

public class FileStorageService(IWebHostEnvironment env)
{
    public async Task<(string storedFileName, long fileSizeBytes)> StoreFileAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(env.WebRootPath, "uploads", storedFileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return (storedFileName, file.Length);
    }

    public string GetFilePath(string storedFileName) =>
        Path.Combine(env.WebRootPath, "uploads", storedFileName);

    public string GetEmrSamplePath(string fileName) =>
        Path.Combine(env.WebRootPath, "emr-samples", fileName);

    public Task<(string storedFileName, long fileSizeBytes)> StoreFromPathAsync(string sourcePath)
    {
        var ext = Path.GetExtension(sourcePath);
        var storedFileName = $"{Guid.NewGuid()}{ext}";
        var destPath = Path.Combine(env.WebRootPath, "uploads", storedFileName);
        File.Copy(sourcePath, destPath);
        return Task.FromResult((storedFileName, new FileInfo(destPath).Length));
    }
}
