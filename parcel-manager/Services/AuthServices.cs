using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ParcelManager.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        private const string LoginUrl =
            "http://127.0.0.1:8000/api/auth/login/";

        private string? _accessToken;
        private string? _refreshToken;

        public bool IsAuthenticated =>
            !string.IsNullOrWhiteSpace(_accessToken);

        public AuthService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // =========================================================
        // LOGIN
        // =========================================================

        public async Task<bool> LoginAsync(
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

                var json = JsonSerializer.Serialize(loginData);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    LoginUrl)
                {
                    Content = content,
                    Version = new Version(1, 1),
                    VersionPolicy =
                        HttpVersionPolicy.RequestVersionExact
                };

                using var response =
                    await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        "Invalid username or password.",
                        "TopMap Solutions",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return false;
                }

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                var result =
                    JsonSerializer.Deserialize<TokenResponse>(
                        responseBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (result == null ||
                    string.IsNullOrWhiteSpace(result.Access))
                {
                    MessageBox.Show(
                        "Login failed. Please try again.",
                        "TopMap Solutions",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return false;
                }

                _accessToken = result.Access;
                _refreshToken = result.Refresh;

                return true;
            }
            catch (HttpRequestException)
            {
                MessageBox.Show(
                    "Could not connect to the server.",
                    "TopMap Solutions",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show(
                    "The request timed out. Please try again.",
                    "TopMap Solutions",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "An unexpected error occurred. Please try again.",
                    "TopMap Solutions",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }

        // =========================================================
        // ACCESS TOKEN
        // =========================================================

        public string? GetAccessToken()
        {
            return _accessToken;
        }

        // =========================================================
        // REFRESH TOKEN
        // =========================================================

        public string? GetRefreshToken()
        {
            return _refreshToken;
        }

        // =========================================================
        // ADD JWT AUTHORIZATION
        // =========================================================

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

        // =========================================================
        // LOGOUT
        // =========================================================

        public void Logout()
        {
            _accessToken = null;
            _refreshToken = null;
        }

        // =========================================================
        // JWT RESPONSE
        // =========================================================

        private sealed class TokenResponse
        {
            public string Access { get; set; } = "";

            public string Refresh { get; set; } = "";
        }
    }
}

