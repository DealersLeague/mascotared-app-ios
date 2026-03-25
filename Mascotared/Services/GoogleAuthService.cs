namespace Mascotared.Services;

public class GoogleAuthService
{
    private const string ClientId = "278243602499-3v8n4gigs46r9udg145moh3jdbq6pqmh.apps.googleusercontent.com";

    public async Task<string?> ObtenerIdTokenAsync()
    {
        try
        {
            var authUrl = new Uri(
                "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={ClientId}" +
                "&response_type=id_token" +
                "&scope=openid%20email%20profile" +
                "&redirect_uri=com.mascotared.app:/oauth2redirect" +
                $"&nonce={Guid.NewGuid()}");

            var callbackUrl = new Uri("com.mascotared.app:/oauth2redirect");
            var result = await WebAuthenticator.Default.AuthenticateAsync(authUrl, callbackUrl);
            result.Properties.TryGetValue("id_token", out var idToken);
            return idToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Google Auth error: {ex.Message}");
            return null;
        }
    }
}