using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using ParcelManager.Models;

namespace ParcelManager.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigService _configService;
        private readonly JsonSerializerOptions _jsonOptions;

        private string? _accessToken;
        private string? _refreshToken;
        private string? _username;

        public bool IsAuthenticated =>
            !string.IsNullOrWhiteSpace(_accessToken);

        public AuthService(
            ConfigService configService)
        {
            _configService = configService;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public string? GetUsername()
        {
            return _username;
        }

        public async Task<LoginResult> LoginAsync(
             string username,
             string password)
        {
            try
            {
                var loginData = new
                {
                    username,
                    password
                };

                var json =
                    JsonSerializer.Serialize(loginData);

                using var content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                string baseUrl =
                    _configService.Config.BaseUrl.TrimEnd('/');

                string loginUrl =
                    $"{baseUrl}/api/auth/login/";

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        loginUrl)
                    {
                        Content = content,
                        Version = new Version(1, 1),
                        VersionPolicy =
                            HttpVersionPolicy.RequestVersionExact
                    };

                using var response =
                    await _httpClient.SendAsync(request);

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return LoginResult.InvalidCredentials;
                }

                if (!response.IsSuccessStatusCode)
                {
                    return LoginResult.ServerUnavailable;
                }

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                var result =
                    JsonSerializer.Deserialize<TokenResponse>(
                        responseBody,
                        _jsonOptions);

                if (result == null ||
                    string.IsNullOrWhiteSpace(result.Access))
                {
                    return LoginResult.Failed;
                }

                _accessToken = result.Access;
                _refreshToken = result.Refresh;
                _username = username;

                return LoginResult.Success;
            }
            catch (HttpRequestException)
            {
                return LoginResult.ServerUnavailable;
            }
            catch (TaskCanceledException)
            {
                return LoginResult.Timeout;
            }
            catch (Exception)
            {
                return LoginResult.Failed;
            }
        }

        public string? GetAccessToken()
        {
            return _accessToken;
        }

        public string? GetRefreshToken()
        {
            return _refreshToken;
        }

        public void AddAuthorizationHeader(
            HttpRequestMessage request)
        {
            if (string.IsNullOrWhiteSpace(_accessToken))
            {
                return;
            }

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _accessToken);
        }

        public void Logout()
        {
            _username = null;
            _accessToken = null;
            _refreshToken = null;
        }
    }
}