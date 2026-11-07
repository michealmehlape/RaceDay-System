using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

// Describes what the upload service can do (so tests can use a fake one)
public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
}

// Uploads images to Azure Blob Storage and returns their public URL
public class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _config;

    public BlobStorageService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        string? connectionString = _config["AzureBlob:ConnectionString"];
        string containerName = _config["AzureBlob:ContainerName"] ?? "raceday-images";

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var container = new BlobContainerClient(connectionString, containerName);

        // Give the file a random name so two uploads never overwrite each other
        string extension = Path.GetExtension(file.FileName).ToLower();
        string blobName = folder + "/" + Guid.NewGuid() + extension;

        var blob = container.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        // The public address of the uploaded file
        return blob.Uri.ToString();
    }
}