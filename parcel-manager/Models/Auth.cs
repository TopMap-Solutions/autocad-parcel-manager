using System;

namespace ParcelManager.Models
{
    public class TokenResponse
    {
        public string Access { get; set; } = "";
        public string Refresh { get; set; } = "";
    }

    public enum LoginResult
    {
        Success,
        InvalidCredentials,
        ServerUnavailable,
        Timeout,
        Failed
    }
}