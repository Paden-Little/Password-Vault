using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using SmwHackTracker.api.DAL;
using SmwHackTracker.api.DAL.Models;
using System.Runtime.CompilerServices;

namespace SmwHackTracker.api;



public class OtpCode
{
    public string Code { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public OtpCode(string code, string Email){ this.Code = code;  this.Email = Email; }
}

public static class OtpStore
{
    private static readonly Dictionary<string, string> store = new();

    public static void Save(string email, string otp) => store[email] = otp;

    public static bool Validate(string email, string otp) =>
        store.TryGetValue(email, out var storedOtp) && storedOtp == otp;
}



public static class AuthEndpoints
{
    private const string StaticSalt = "TheSaltiestSaltThatEverSalted"; // TODO: Generate a random salt


    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        string GenerateOtp() => new Random().Next(100000, 999999).ToString();

        app.MapPost("/auth/login", async (HttpContext context, UserRepository repo) =>
        {
            var request = await JsonSerializer.DeserializeAsync<LoginRequest>(context.Request.Body);
            if (request is null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request");
                return;
            }

            var hashedPassword = HashPassword(request.Password, StaticSalt);
            var user = await repo.LoginAsync(request.Username, hashedPassword);

            if (user is null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid credentials");
                return;
            }

            await context.Response.WriteAsJsonAsync(new { user.Id, user.Username, user.Email });


            OtpCode otp = new OtpCode(GenerateOtp(), user.Email);
            OtpStore.Save(user.Email, otp.Code);
            var emailSender = context.RequestServices.GetRequiredService<EmailService>();
            await emailSender.SendEmailAsync(user.Email, "Your One Time Password", otp.Code);

            return;
        });

        app.MapPost("/auth/create", async (HttpContext context, UserRepository repo) =>
        {
            var request = await JsonSerializer.DeserializeAsync<CreateRequest>(context.Request.Body);
            if (request is null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request");
                return;
            }

            if (!Regex.IsMatch(request.Password, "^(?=.*[A-Za-z])(?=.*\\d)[A-Za-z\\d]{8,}$"))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Password must be at least 8 characters, include a letter and a number.");
                return;
            }

            var hashedPassword = HashPassword(request.Password, StaticSalt);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                Password = hashedPassword
            };

            await repo.CreateUserAsync(user);

            await context.Response.WriteAsJsonAsync(new { user.Id, user.Username, user.Email });

            OtpCode otp = new OtpCode(GenerateOtp(), user.Email);
            OtpStore.Save(user.Email, otp.Code);
            var emailSender = context.RequestServices.GetRequiredService<EmailService>();
            await emailSender.SendEmailAsync(user.Email, "Your One Time Password", otp.Code);

            return;
        });

        app.MapPost("/auth/otp", async (HttpContext context) =>
        {
            var request = await JsonSerializer.DeserializeAsync<OtpCode>(context.Request.Body);
            if (request is null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Code))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid OTP request");
                return;
            }

            if (OtpStore.Validate(request.Email, request.Code))
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("OTP verified successfully");
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or expired OTP");
            }
        });
    }

    private static string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = salt + password;
        var bytes = Encoding.UTF8.GetBytes(saltedPassword);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private record LoginRequest(string Username, string Password);
    private record CreateRequest(string Username, string Email, string Password);
}