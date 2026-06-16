using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Habitu.Application.Abstractions;

namespace Habitu.Infrastructure.Services;

public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseApiKey;

    public SupabaseStorageService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseUrl = configuration["Supabase:Url"] ?? string.Empty;
        _supabaseApiKey = configuration["Supabase:ApiKey"] ?? string.Empty;
    }

    public async Task<string> UploadFileAsync(string bucketName, string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        var baseUrl = _supabaseUrl.TrimEnd('/');
        var uploadUrl = $"{baseUrl}/storage/v1/object/{bucketName}/{fileName}";

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        request.Headers.Add("apikey", _supabaseApiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseApiKey);

        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = streamContent;

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var publicUrl = $"{baseUrl}/storage/v1/object/public/{bucketName}/{fileName}";
        return publicUrl;
    }
}