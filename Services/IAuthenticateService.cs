using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;

namespace GmailHandler.Services
{
    public interface IAuthenticateService
    {
        public string GenerateUrl();
        public Task ExchangeCodeForTokenAsync(string code);

        public void StoreTokenInCookies(TokenResponse token);
        public TokenResponse GetTokensFromCookies();
        public Task<bool> IsValidToken();
        public GmailService getServiceFromToken();
        public void LogOut();
    }
}
