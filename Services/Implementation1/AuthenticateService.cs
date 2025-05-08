using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Newtonsoft.Json.Linq;

namespace GmailHandler.Services.Implementation1
{
    public class AuthenticateService : IAuthenticateService
    {
        private readonly IConfiguration Configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private string CLIENT_ID;
        private string CLIENT_SECRET;
        private string REDIRECT_URL;
        private readonly string[] Scopes = { "https://mail.google.com/" };
        private string credentialsFilePath;

        public AuthenticateService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            Configuration = configuration;
            credentialsFilePath = Configuration["CredentialPath"];
            _httpContextAccessor = httpContextAccessor;
        }

        private void LoadCredentials()
        {
            var json = File.ReadAllText(credentialsFilePath);
            var parsed = JObject.Parse(json);
            var section = parsed["installed"] ?? parsed["web"];

            CLIENT_ID = section["client_id"]?.ToString();
            CLIENT_SECRET = section["client_secret"]?.ToString();
            REDIRECT_URL = section["redirect_uris"]?.First?.ToString();
        }
        private void ClearAllCookies(HttpContext context)
        {
            foreach (var cookie in context.Request.Cookies.Keys)
            {
                context.Response.Cookies.Delete(cookie);
            }
        }


        public string GenerateUrl()
        {
            LoadCredentials(); // Ensure credentials are loaded before generating URL

            string baseUrl = "https://accounts.google.com/o/oauth2/v2/auth";

            var queryParams = new Dictionary<string, string>
            {
                { "client_id", CLIENT_ID },
                { "redirect_uri", REDIRECT_URL },
                { "response_type", "code" },
                { "scope", string.Join(" ", Scopes) },
                { "access_type", "offline" },
                { "prompt", "consent" },
                { "include_granted_scopes", "true" }
            };

            var query = string.Join("&", queryParams.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"
            ));

            return $"{baseUrl}?{query}";
        }


        public async Task ExchangeCodeForTokenAsync(string code)
        {
            LoadCredentials(); // Make sure CLIENT_ID, CLIENT_SECRET, and REDIRECT_URL are loaded

            var tokenResponse = await new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = CLIENT_ID,
                    ClientSecret = CLIENT_SECRET
                },
                Scopes = Scopes
            }).ExchangeCodeForTokenAsync(
                userId: "me", // can be any string if you're not storing tokens per user
                code: code,
                redirectUri: REDIRECT_URL,
                taskCancellationToken: CancellationToken.None);

            StoreTokenInCookies(tokenResponse); // Your custom method to store cookies
        }

        public void StoreTokenInCookies(TokenResponse token)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            };

            context.Response.Cookies.Append("access_token", token.AccessToken, options);
            context.Response.Cookies.Append("refresh_token", token.RefreshToken ?? "", options);

            var expiry = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds ?? 3600);
            context.Response.Cookies.Append("token_expires_at", expiry.ToString("o"), options);
        }


        public GmailService getServiceFromToken()
        {
            TokenResponse token = GetTokensFromCookies();

            if (token == null || string.IsNullOrEmpty(token.AccessToken))
            {
                throw new InvalidOperationException("Access token is missing or invalid.");
            }

            // Create a GoogleCredential from the token
            var credential = GoogleCredential.FromAccessToken(token.AccessToken);

            // Create and return a new GmailService using the GoogleCredential
            var gmailService = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Gmail Handler"  // Set your app's name
            });

            return gmailService;
        }
        public TokenResponse GetTokensFromCookies()
        {
            var context = _httpContextAccessor.HttpContext;

            if (context == null || !context.Request.Cookies.ContainsKey("access_token"))
                return null;

            string expiresAtRaw = context.Request.Cookies["token_expires_at"];
            string decodedExpiresAt = Uri.UnescapeDataString(expiresAtRaw);

            long expiresInSeconds = 0;
            if (DateTime.TryParse(decodedExpiresAt, out DateTime expiresAt))
            {
                var timeLeft = expiresAt - DateTime.UtcNow;
                expiresInSeconds = (long)Math.Max(timeLeft.TotalSeconds, 0); // Don't return negative values
            }

            return new TokenResponse
            {
                AccessToken = context.Request.Cookies["access_token"],
                RefreshToken = context.Request.Cookies["refresh_token"],
                ExpiresInSeconds = expiresInSeconds
            };
        }

        public async Task<bool> IsValidToken()
        {
            // Get the tokens and expiration time from cookies
            var token = GetTokensFromCookies();

            if (token == null || string.IsNullOrEmpty(token.AccessToken))
            {
                return false;  // No token or access token not available
            }

            // Calculate the token expiration time
            DateTime tokenExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds ?? 0); // Or use your stored expiration DateTime
            DateTime currentTime = DateTime.UtcNow;  // Get the current UTC time

            // If the token is expired, use the refresh token to get a new access token
            if (tokenExpiresAt <= currentTime)
            {
                if (string.IsNullOrEmpty(token.RefreshToken))
                {
                    return false;  // No refresh token available, cannot refresh the access token
                }
                Console.WriteLine(token.RefreshToken);
                Console.WriteLine(token.ToString());
                // Attempt to refresh the token
                var newToken = await RefreshAccessToken(token.RefreshToken);

                if (newToken == null)
                {
                    return false;  // Failed to refresh the token
                }

                // Store the new token in cookies or your storage
                StoreTokenInCookies(newToken);  // Implement this method to store the new tokens

                return true;  // Successfully refreshed the token
            }

            return true;  // Token is still valid
        }

        private async Task<TokenResponse> RefreshAccessToken(string refreshToken)
        {
            // Create a new GoogleOAuth2 client using your client ID and client secret
            
            var tokenRequest = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = CLIENT_ID,
                    ClientSecret =CLIENT_SECRET
                }
            });
            
            var cancellationToken = CancellationToken.None;
            // Request the new access token using the refresh token (asynchronously)
            var newToken = await tokenRequest.RefreshTokenAsync( CLIENT_ID, refreshToken, cancellationToken);
            if (newToken == null)
            {
                return null;  // Failed to get a new token
            }

            // Return the new token
            return new TokenResponse
            {
                AccessToken = newToken.AccessToken,
                RefreshToken = newToken.RefreshToken,
                ExpiresInSeconds = newToken.ExpiresInSeconds,                
            };
        }


        public void LogOut()
        {
            ClearAllCookies(_httpContextAccessor.HttpContext);
        }
    }
}
