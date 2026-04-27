using Microsoft.EntityFrameworkCore;
using Npgsql;
using TravelAgency.Core.Data.Entities;
using TravelAgency.Core.Data;
using TravelAgency.Core.Models.Users.Access;
using TravelAgency.Core.Services;
using TravelAgency.Core.Services.Email;
using TravelAgency.Core.Services.Sms;
using System.Security.Cryptography;
using System.Text;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<TravelAgencyDbContext>(options =>
    options.UseNpgsql(BuildConnectionString(builder.Configuration)));

var smtp = builder.Configuration.GetSection("Smtp");
builder.Services.AddSingleton<IEmailSender>(_ =>
{
    var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
    var ssl = !string.Equals(smtp["Ssl"], "false", StringComparison.OrdinalIgnoreCase);
    var user = smtp["User"] ?? Environment.GetEnvironmentVariable("SMTP_USER");
    var pass = smtp["Pass"] ?? Environment.GetEnvironmentVariable("SMTP_PASS");

    return new SmtpEmailSender(
        smtp["Host"] ?? "",
        smtp["From"],
        user,
        pass,
        port,
        ssl);
});

var twilio = builder.Configuration.GetSection("Twilio");
builder.Services.AddSingleton<ISmsSender>(_ =>
{
    var sid = twilio["AccountSid"] ?? Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
    var token = twilio["AuthToken"] ?? Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
    var from = twilio["FromPhoneNumber"] ?? Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE");
    return new TwilioSmsSender(sid, token, from);
});

var publicBaseUrl = builder.Configuration["PasswordReset:PublicBaseUrl"] ?? "http://localhost:5280";

builder.Services.AddScoped<IPasswordResetFlow>(sp =>
    new PasswordResetFlowService(
        () => sp.GetRequiredService<IDbContextFactory<TravelAgencyDbContext>>().CreateDbContext(),
        sp.GetRequiredService<IEmailSender>(),
        sp.GetRequiredService<ISmsSender>(),
        publicBaseUrl));

var app = builder.Build();

app.MapPost("/api/password-reset/request", RequestPasswordResetAsync);
app.MapPost("/api/password-reset/confirm", ConfirmPasswordResetAsync);
app.MapGet("/reset-password", ResetPasswordPage);
app.MapPost("/reset-password", ResetPasswordSubmit);

app.Run();

static async Task<IResult> RequestPasswordResetAsync(
    PasswordResetRequestApiModel body,
    IPasswordResetFlow flow,
    CancellationToken cancellationToken)
{
    try
    {
        var sent = await flow.RequestResetAsync(body.EmailOrUsername, cancellationToken);
        return Results.Ok(new PasswordResetRequestApiResponse { CodeSent = sent });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new PasswordResetErrorApiResponse { Error = ex.Message });
    }
    catch (Twilio.Exceptions.ApiException ex)
    {
        return Results.BadRequest(new PasswordResetErrorApiResponse
        {
            Error = "SMS could not be sent (Twilio). Server: " + ex.Message
        });
    }
}

static async Task<IResult> ConfirmPasswordResetAsync(
    PasswordResetConfirmApiModel body,
    IPasswordResetFlow flow,
    CancellationToken cancellationToken)
{
    try
    {
        await flow.ConfirmResetAsync(body.EmailOrUsername, body.OtpCode, body.NewPassword, cancellationToken);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new PasswordResetErrorApiResponse { Error = ex.Message });
    }
}

static IResult ResetPasswordPage(HttpRequest request)
{
    var token = request.Query["token"].ToString();
    var safeToken = WebUtility.HtmlEncode(token ?? "");
    var error = request.Query["error"].ToString();
    var safeError = string.IsNullOrWhiteSpace(error) ? "" : WebUtility.HtmlEncode(error);

    var html = $@"
<!doctype html>
<html lang=""ro"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Resetare parolă</title>
  <style>
    body {{ font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif; background:#0B1220; margin:0; padding:28px; color:#E2E8F0; }}
    .card {{ max-width:520px; margin:0 auto; background:rgba(255,255,255,.10); border:1px solid rgba(255,255,255,.18); border-radius:16px; padding:20px; }}
    h1 {{ margin:0 0 10px 0; font-size:22px; color:#fff; }}
    p {{ margin:0 0 18px 0; opacity:.9; }}
    .error {{ margin: 0 0 14px 0; padding: 10px 12px; border-radius: 12px; background: rgba(248,113,113,.14); border:1px solid rgba(248,113,113,.25); color:#FCA5A5; font-size:13px; }}
    label {{ display:block; font-size:12px; font-weight:600; margin:12px 0 6px; }}
    input {{ width:100%; height:38px; border-radius:12px; border:1px solid #334155; padding:0 12px; font-size:14px; }}
    button {{ margin-top:16px; width:100%; height:40px; border-radius:12px; border:1px solid rgba(255,255,255,.25); background:rgba(255,255,255,.15); color:#fff; font-weight:700; cursor:pointer; }}
    .muted {{ font-size:12px; opacity:.8; }}
  </style>
  </head>
<body>
  <div class=""card"">
    <h1>Resetare parolă</h1>
    <p>Setează o parolă nouă pentru contul tău.</p>
    {(string.IsNullOrWhiteSpace(safeError) ? "" : $@"<div class=""error"">{safeError}</div>")}
    <form method=""post"" action=""/reset-password"">
      <input type=""hidden"" name=""token"" value=""{safeToken}"" />
      <label>Parolă nouă</label>
      <input type=""password"" name=""newPassword"" required minlength=""8"" />
      <label>Confirmă parola</label>
      <input type=""password"" name=""confirmPassword"" required minlength=""8"" />
      <button type=""submit"">Resetează parola</button>
      <div class=""muted"" style=""margin-top:10px;"">Dacă linkul a expirat, cere unul nou din aplicație.</div>
    </form>
  </div>
</body>
</html>";

    return Results.Content(html, "text/html; charset=utf-8");
}

static async Task<IResult> ResetPasswordSubmit(
    HttpRequest request,
    IDbContextFactory<TravelAgencyDbContext> dbFactory,
    CancellationToken cancellationToken)
{
    var form = await request.ReadFormAsync(cancellationToken);
    var token = (form["token"].ToString() ?? "").Trim();
    var newPassword = form["newPassword"].ToString() ?? "";
    var confirm = form["confirmPassword"].ToString() ?? "";

    if (string.IsNullOrWhiteSpace(token))
        return Results.Content(SimpleMessage("Token lipsă sau invalid."), "text/html; charset=utf-8", statusCode: 400);
    if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        return Results.Redirect($"/reset-password?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString("Parola trebuie să aibă minim 8 caractere.")}");
    if (!string.Equals(newPassword, confirm, StringComparison.Ordinal))
        return Results.Redirect($"/reset-password?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString("Parolele nu se potrivesc.")}");

    using var db = dbFactory.CreateDbContext();

    var now = DateTime.UtcNow;
    var tokenHash = HashToken(token);

    var linkToken = await db.PasswordResetLinkTokens
        .Where(t => t.TokenHash == tokenHash && t.ConsumedAtUtc == null && t.ExpiresAtUtc > now)
        .OrderByDescending(t => t.CreatedAtUtc)
        .FirstOrDefaultAsync(cancellationToken);

    if (linkToken == null)
        return Results.Content(SimpleMessage("Link invalid sau expirat. Te rog cere un link nou din aplicație."), "text/html; charset=utf-8", statusCode: 400);

    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == linkToken.UserId, cancellationToken);
    if (user == null)
        return Results.Content(SimpleMessage("Link invalid."), "text/html; charset=utf-8", statusCode: 400);

    user.PasswordHash = PasswordHasher.Hash(newPassword);
    linkToken.ConsumedAtUtc = now;
    await db.SaveChangesAsync(cancellationToken);

    return Results.Content(SimpleMessage("Parola a fost schimbată cu succes. Te poți întoarce în aplicație și te poți autentifica."), "text/html; charset=utf-8");
}

static string HashToken(string token)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(token ?? "");
    var hashBytes = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hashBytes);
}

static string SimpleMessage(string message)
{
    var msg = WebUtility.HtmlEncode(message ?? "");
    return $@"
<!doctype html>
<html lang=""ro""><head><meta charset=""utf-8"" /><meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
<title>Resetare parolă</title>
<style>
 body {{ font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif; background:#0B1220; color:#E2E8F0; padding:28px; }}
 .card {{ max-width:520px; margin:0 auto; background:rgba(255,255,255,.10); border:1px solid rgba(255,255,255,.18); border-radius:16px; padding:20px; }}
 h1 {{ margin:0 0 10px 0; font-size:22px; color:#fff; }}
 p {{ margin:0; opacity:.9; }}
</style></head>
<body><div class=""card""><h1>Resetare parolă</h1><p>{msg}</p></div></body></html>";
}

static string BuildConnectionString(IConfiguration configuration)
{
    var baseConn = configuration.GetConnectionString("TravelAgencyDb")
        ?? throw new InvalidOperationException("Set ConnectionStrings:TravelAgencyDb in appsettings or user secrets.");

    var password = Environment.GetEnvironmentVariable("TRAVEL_AGENCY_DB_PASSWORD");
    if (!string.IsNullOrWhiteSpace(password))
    {
        var merged = new NpgsqlConnectionStringBuilder(baseConn)
        {
            Password = password
        };
        return merged.ConnectionString;
    }

    var parsed = new NpgsqlConnectionStringBuilder(baseConn);
    if (string.IsNullOrWhiteSpace(parsed.Password))
    {
        throw new InvalidOperationException(
            "Database password missing: include Password in ConnectionStrings:TravelAgencyDb or set TRAVEL_AGENCY_DB_PASSWORD.");
    }

    return baseConn;
}
