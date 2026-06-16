using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Habitu.Application.Abstractions;

public interface ISupabaseStorageService
{
    Task<string> UploadFileAsync(string bucketName, string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
}