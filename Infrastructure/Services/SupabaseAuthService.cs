using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;

namespace Habitu.Infrastructure.Services;

public class SupabaseAuthService : ISupabaseAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseApiKey;

    public SupabaseAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseUrl = configuration["Supabase:Url"] ?? string.Empty;
        _supabaseApiKey = configuration["Supabase:ApiKey"] ?? string.Empty;
    }

    private void AddSupabaseHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("apikey", _supabaseApiKey);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var baseUrl = _supabaseUrl.TrimEnd('/');
        var signupUrl = $"{baseUrl}/auth/v1/signup";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, signupUrl);
        AddSupabaseHeaders(httpRequest);
        httpRequest.Content = JsonContent.Create(new
        {
            email = request.Email,
            password = request.Password
        });

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Supabase register failed: {err}");
        }

        var session = await response.Content.ReadFromJsonAsync<SupabaseSessionResponse>(cancellationToken: cancellationToken);
        if (session?.User == null)
        {
            throw new Exception("Supabase register returned empty user");
        }

        return new AuthResponseDto(
            AccessToken: session.AccessToken ?? "",
            RefreshToken: session.RefreshToken,
            UserId: session.User.Id,
            Email: session.User.Email ?? request.Email,
            FullName: request.FullName,
            AvatarUrl: null,
            Role: "student"
        );
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var baseUrl = _supabaseUrl.TrimEnd('/');
        var loginUrl = $"{baseUrl}/auth/v1/token?grant_type=password";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, loginUrl);
        AddSupabaseHeaders(httpRequest);
        httpRequest.Content = JsonContent.Create(new
        {
            email = request.Email,
            password = request.Password
        });

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Supabase login failed: {err}");
        }

        var session = await response.Content.ReadFromJsonAsync<SupabaseSessionResponse>(cancellationToken: cancellationToken);
        if (session?.User == null)
        {
            throw new Exception("Supabase login returned empty user");
        }

        return new AuthResponseDto(
            AccessToken: session.AccessToken ?? "",
            RefreshToken: session.RefreshToken,
            UserId: session.User.Id,
            Email: session.User.Email ?? request.Email,
            FullName: "",
            AvatarUrl: null,
            Role: "student"
        );
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var baseUrl = _supabaseUrl.TrimEnd('/');
        var refreshUrl = $"{baseUrl}/auth/v1/token?grant_type=refresh_token";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, refreshUrl);
        AddSupabaseHeaders(httpRequest);
        httpRequest.Content = JsonContent.Create(new
        {
            refresh_token = request.RefreshToken
        });

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Supabase token refresh failed: {err}");
        }

        var session = await response.Content.ReadFromJsonAsync<SupabaseSessionResponse>(cancellationToken: cancellationToken);
        if (session?.User == null)
        {
            throw new Exception("Supabase token refresh returned empty user");
        }

        return new AuthResponseDto(
            AccessToken: session.AccessToken ?? "",
            RefreshToken: session.RefreshToken,
            UserId: session.User.Id,
            Email: session.User.Email ?? "",
            FullName: "",
            AvatarUrl: null,
            Role: "student"
        );
    }
}

public class SupabaseSessionResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("user")]
    public SupabaseUser? User { get; set; }
}

public class SupabaseUser
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}