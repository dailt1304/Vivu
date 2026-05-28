using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.Interfaces.Files;

namespace Vivu.Infrastructure.Services.Files
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _logger = logger;

            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new ArgumentException("Cloudinary configuration is missing. Please check Cloudinary:CloudName, Cloudinary:ApiKey, and Cloudinary:ApiSecret in appsettings.json");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true; // Use HTTPS URLs
        }

        public async Task<string> UploadImageAsync(string base64Image, string folder = "locations", string? publicId = null)
        {
            try
            {
                // Remove data:image/xxx;base64, prefix if exists
                var base64Data = base64Image;
                if (base64Image.Contains(","))
                {
                    base64Data = base64Image.Split(',')[1];
                }

                var imageBytes = Convert.FromBase64String(base64Data);

                using var stream = new MemoryStream(imageBytes);
                return await UploadImageAsync(stream, Guid.NewGuid().ToString(), folder, publicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image to Cloudinary from base64. Folder: {Folder}", folder);
                throw;
            }
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string folder = "locations", string? publicId = null)
        {
            try
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(fileName, imageStream),
                    Folder = folder,
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto"),
                    UseFilename = false,
                    UniqueFilename = true,
                    Overwrite = false
                };

                if (!string.IsNullOrWhiteSpace(publicId))
                {
                    uploadParams.PublicId = $"{folder}/{publicId}";
                }

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                    throw new Exception($"Failed to upload image to Cloudinary: {uploadResult.Error.Message}");
                }

                _logger.LogInformation("Image uploaded successfully to Cloudinary. URL: {Url}, PublicId: {PublicId}", uploadResult.SecureUrl, uploadResult.PublicId);
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image to Cloudinary. FileName: {FileName}, Folder: {Folder}", fileName, folder);
                throw;
            }
        }

        public async Task<List<string>> UploadImagesAsync(List<string> base64Images, string folder = "locations")
        {
            var uploadedUrls = new List<string>();

            foreach (var base64Image in base64Images)
            {
                try
                {
                    var url = await UploadImageAsync(base64Image, folder);
                    uploadedUrls.Add(url);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload one of the images in batch upload");
                    // Continue with other images even if one fails
                }
            }

            return uploadedUrls;
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            try
            {
                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);

                if (result.Result == "ok" || result.Result == "not found")
                {
                    _logger.LogInformation("Image deleted successfully from Cloudinary. PublicId: {PublicId}", publicId);
                    return true;
                }

                _logger.LogWarning("Failed to delete image from Cloudinary. PublicId: {PublicId}, Result: {Result}", publicId, result.Result);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from Cloudinary. PublicId: {PublicId}", publicId);
                return false;
            }
        }

        public async Task<bool> DeleteImagesAsync(List<string> publicIds)
        {
            var allSuccess = true;

            foreach (var publicId in publicIds)
            {
                var success = await DeleteImageAsync(publicId);
                if (!success)
                {
                    allSuccess = false;
                }
            }

            return allSuccess;
        }
    }
}
