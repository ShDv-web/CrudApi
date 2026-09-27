// Validates a bearer token on every request and short-circuits with 401 when missing or invalid.
public class TokenAuthenticationMiddleware
{
    private const string BearerPrefix = "Bearer ";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public TokenAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var expectedToken = _configuration["ApiToken"];
        var providedToken = ExtractToken(context.Request);

        if (string.IsNullOrEmpty(expectedToken) || providedToken != expectedToken)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized: missing or invalid token." });
            return;
        }

        await _next(context);
    }

    private static string? ExtractToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[BearerPrefix.Length..]
            : null;
    }
}

public static class TokenAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenAuthentication(this IApplicationBuilder app) =>
        app.UseMiddleware<TokenAuthenticationMiddleware>();
}
