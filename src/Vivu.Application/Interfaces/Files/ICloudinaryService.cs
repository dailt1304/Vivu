using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.Interfaces.Files
{
    public interface ICloudinaryService
    {

        Task<string> UploadImageAsync(string base64Image, string folder = "locations", string? publicId = null);
        Task<string> UploadImageAsync(Stream imageStream, string fileName, string folder = "locations", string? publicId = null);
        Task<List<string>> UploadImagesAsync(List<string> base64Images, string folder = "locations");
        Task<bool> DeleteImageAsync(string publicId);
        Task<bool> DeleteImagesAsync(List<string> publicIds);
    }
}
