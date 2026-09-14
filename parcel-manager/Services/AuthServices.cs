using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace ParcelManager.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        private const string LoginUrl =
            "http://127.0.0.1:8000/api/auth/login/";

        private const string RefreshUrl =
            "http://127.0.0.1:8000/api/auth/refresh/";

        private string? _accessToken;
        private string? _refreshToken;

        public bool IsAuthenticated =>
            !string.IsNullOrWhiteSpace(_accessToken);

        public AuthService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> LoginAsync(
            string username,
            string password)
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    LoginUrl,
                    new
                    {
                        username,
                        password
                    });

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result =
                await response.Content.ReadFromJsonAsync<TokenResponse>();

            if (result == null)
            {
                return false;
            }

            _accessToken = result.Access;
            _refreshToken = result.Refresh;

            return true;
        }

        public string? GetAccessToken()
        {
            return _accessToken;
        }

        public void Logout()
        {
            _accessToken = null;
            _refreshToken = null;
        }

        private class TokenResponse
        {
            public string Access { get; set; } = "";
            public string Refresh { get; set; } = "";
        }
    }
}