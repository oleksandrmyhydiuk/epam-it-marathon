using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RestaurantAqaFramework.Config;
using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Core;

namespace RestaurantAqaFramework.Screenplay.Abilities;

/// <summary>
/// Screenplay Ability allowing an Actor to interact directly with the Restaurant Backend REST API.
/// Supports Cognito JWT Bearer tokens as well as Guest Session X-Anonymous-Token headers.
/// </summary>
public sealed class CallRestApi : IAbility
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;

    public string BaseUrl { get; }
    public string? BearerToken { get; private set; }
    public string? GuestAnonymousToken { get; private set; }
    public string? Email { get; private set; }
    public string? Password { get; private set; }

    public CallRestApi(string? baseUrl = null)
    {
        BaseUrl = (baseUrl ?? TestSettings.Current.ApiBaseUrl).TrimEnd('/');
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(TestSettings.Current.DefaultTimeoutMs)
        };
    }

    public static CallRestApi At(string? baseUrl = null) => new(baseUrl);

    public CallRestApi WithBearerToken(string? token, string? email = null, string? password = null)
    {
        BearerToken = token;
        if (email is not null) Email = email;
        if (password is not null) Password = password;
        return this;
    }

    public CallRestApi WithGuestSessionToken(string? sessionToken)
    {
        GuestAnonymousToken = sessionToken;
        return this;
    }

    public async Task<ApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string relativePath,
        object? requestBody = null,
        bool includeAuth = true,
        bool useGuestToken = false,
        IDictionary<string, string>? customHeaders = null)
    {
        var url = $"{BaseUrl}/{relativePath.TrimStart('/')}";
        using var request = new HttpRequestMessage(method, url);

        if (includeAuth && !string.IsNullOrWhiteSpace(BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
        }

        if (useGuestToken && !string.IsNullOrWhiteSpace(GuestAnonymousToken))
        {
            request.Headers.TryAddWithoutValidation("X-Anonymous-Token", GuestAnonymousToken);
            request.Headers.TryAddWithoutValidation("x-session-token", GuestAnonymousToken);
        }

        if (customHeaders is not null)
        {
            foreach (var kv in customHeaders)
            {
                request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            }
        }

        if (requestBody is not null)
        {
            var json = requestBody is string rawJson
                ? rawJson
                : JsonSerializer.Serialize(requestBody, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request);
        var rawBody = await response.Content.ReadAsStringAsync();

        TResponse? data = default;
        string? errorMessage = null;

        if (!string.IsNullOrWhiteSpace(rawBody))
        {
            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                        errorMessage = msgProp.GetString();
                    else if (doc.RootElement.TryGetProperty("error", out var errProp) && errProp.ValueKind == JsonValueKind.String)
                        errorMessage = errProp.GetString();
                }
            }
            catch
            {
                errorMessage = rawBody;
            }

            if ((int)response.StatusCode is >= 200 and < 300)
            {
                try
                {
                    data = JsonSerializer.Deserialize<TResponse>(rawBody, JsonOptions);
                }
                catch
                {
                    // Ignore deserialization mismatch when TResponse is object/string
                }
            }
        }

        return new ApiResult<TResponse>(response.StatusCode, rawBody, data, errorMessage ?? rawBody);
    }

    public Task<ApiResult<TResponse>> GetAsync<TResponse>(
        string relativePath,
        bool includeAuth = true,
        bool useGuestToken = false) =>
        SendAsync<TResponse>(HttpMethod.Get, relativePath, null, includeAuth, useGuestToken);

    public Task<ApiResult<TResponse>> PostAsync<TResponse>(
        string relativePath,
        object? body = null,
        bool includeAuth = true,
        bool useGuestToken = false) =>
        SendAsync<TResponse>(HttpMethod.Post, relativePath, body, includeAuth, useGuestToken);

    public Task<ApiResult<TResponse>> DeleteAsync<TResponse>(
        string relativePath,
        object? body = null,
        bool includeAuth = true,
        bool useGuestToken = false) =>
        SendAsync<TResponse>(HttpMethod.Delete, relativePath, body, includeAuth, useGuestToken);

    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}

