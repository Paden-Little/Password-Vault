using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using SmwHackTracker.api.DAL;
using SmwHackTracker.api.DAL.Models;

namespace SmwHackTracker.api;
public static class AuthEndpoints
{
    private const string StaticSalt = "TheSaltiestSaltThatEverSalted"; // TODO: Generate a random salt

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
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

            Console.WriteLine(request.ToString());

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