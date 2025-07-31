using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using SmwHackTracker.api.DAL;
using SmwHackTracker.api.DAL.Models;
using SmwHackTracker.api.Utils;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace SmwHackTracker.api;

public static class PasswordEndpoints
{
    public static void MapPasswordEndpoints(this IEndpointRouteBuilder app)
    {
        // Create a new password entry
        app.MapPost("/passwords", async (HttpContext context, PasswordRepository passwordRepo, UserRepository userRepo) =>
        {
            var request = await JsonSerializer.DeserializeAsync<CreatePasswordRequest>(context.Request.Body);
            if (request is null || !IsValidCreateRequest(request))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid request. All fields are required." });
                return;
            }

            try
            {
                // Verify user exists and get master password for encryption
                var user = await userRepo.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new { error = "User not found" });
                    return;
                }

                // Derive encryption key from master password
                var encryptionKey = CryptographyService.DeriveKeyFromMasterPassword(request.MasterPassword);
                
                // Encrypt the password using AES
                var encryptedPassword = CryptographyService.EncryptPassword(request.PlainTextPassword, encryptionKey);

                var password = new Password
                {
                    Platform = request.Platform,
                    Username = request.Username,
                    EncryptedPassword = encryptedPassword,
                    Comment = request.Comment ?? string.Empty,
                    UserId = request.UserId
                };

                var passwordId = await passwordRepo.CreatePasswordAsync(password);

                await context.Response.WriteAsJsonAsync(new { 
                    id = passwordId,
                    message = "Password entry created successfully",
                    platform = password.Platform,
                    username = password.Username
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = $"Failed to create password entry: {ex.Message}" });
            }
        });

        // Get all password entries for a user (with decryption)
        app.MapPost("/passwords/vault", async (HttpContext context, PasswordRepository passwordRepo, UserRepository userRepo) =>
        {
            var request = await JsonSerializer.DeserializeAsync<VaultAccessRequest>(context.Request.Body);
            if (request is null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                return;
            }

            try
            {
                // Verify user credentials
                var user = await userRepo.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new { error = "User not found" });
                    return;
                }

                // Derive decryption key from master password
                var decryptionKey = CryptographyService.DeriveKeyFromMasterPassword(request.MasterPassword);

                // Get all encrypted passwords
                var encryptedPasswords = await passwordRepo.GetAllPasswordsAsync(request.UserId);
                
                // Decrypt passwords for display
                var decryptedVault = encryptedPasswords.Select(p => new VaultEntry(
                    p.Id,
                    p.Platform,
                    p.Username,
                    CryptographyService.DecryptPassword(p.EncryptedPassword, decryptionKey),
                    p.Comment,
                    p.CreatedAt,
                    p.UpdatedAt
                )).ToList();

                await context.Response.WriteAsJsonAsync(new { 
                    vault = decryptedVault,
                    totalEntries = decryptedVault.Count,
                    message = "Vault accessed successfully"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = $"Failed to access vault: {ex.Message}" });
            }
        });

        // Get specific password entry
        app.MapPost("/passwords/{id}", async (HttpContext context, PasswordRepository passwordRepo) =>
        {
            if (!Guid.TryParse(context.Request.RouteValues["id"]?.ToString(), out var passwordId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid password ID" });
                return;
            }

            var request = await JsonSerializer.DeserializeAsync<VaultAccessRequest>(context.Request.Body);
            if (request is null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                return;
            }

            try
            {
                var password = await passwordRepo.GetPasswordByIdAsync(passwordId, request.UserId);
                if (password is null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new { error = "Password entry not found" });
                    return;
                }

                // Decrypt password for display
                var decryptionKey = CryptographyService.DeriveKeyFromMasterPassword(request.MasterPassword);
                var decryptedPassword = CryptographyService.DecryptPassword(password.EncryptedPassword, decryptionKey);

                var result = new VaultEntry(
                    password.Id,
                    password.Platform,
                    password.Username,
                    decryptedPassword,
                    password.Comment,
                    password.CreatedAt,
                    password.UpdatedAt
                );

                await context.Response.WriteAsJsonAsync(result);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = $"Failed to retrieve password: {ex.Message}" });
            }
        });

        // Update password entry
        app.MapPut("/passwords/{id}", async (HttpContext context, PasswordRepository passwordRepo) =>
        {
            if (!Guid.TryParse(context.Request.RouteValues["id"]?.ToString(), out var passwordId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid password ID" });
                return;
            }

            var request = await JsonSerializer.DeserializeAsync<UpdatePasswordRequest>(context.Request.Body);
            if (request is null || !IsValidUpdateRequest(request))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                return;
            }

            try
            {
                // Verify password entry exists and belongs to user
                var existingPassword = await passwordRepo.GetPasswordByIdAsync(passwordId, request.UserId);
                if (existingPassword is null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new { error = "Password entry not found" });
                    return;
                }

                // Encrypt updated password
                var encryptionKey = CryptographyService.DeriveKeyFromMasterPassword(request.MasterPassword);
                var encryptedPassword = CryptographyService.EncryptPassword(request.PlainTextPassword, encryptionKey);

                existingPassword.Platform = request.Platform;
                existingPassword.Username = request.Username;
                existingPassword.EncryptedPassword = encryptedPassword;
                existingPassword.Comment = request.Comment ?? string.Empty;

                var success = await passwordRepo.UpdatePasswordAsync(existingPassword);
                if (success)
                {
                    await context.Response.WriteAsJsonAsync(new { 
                        message = "Password entry updated successfully",
                        platform = existingPassword.Platform,
                        username = existingPassword.Username
                    });
                }
                else
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Failed to update password entry" });
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = $"Failed to update password: {ex.Message}" });
            }
        });

        // Delete password entry
        app.MapDelete("/passwords/{id}", async (HttpContext context, PasswordRepository passwordRepo) =>
        {
            if (!Guid.TryParse(context.Request.RouteValues["id"]?.ToString(), out var passwordId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid password ID" });
                return;
            }

            var request = await JsonSerializer.DeserializeAsync<DeletePasswordRequest>(context.Request.Body);
            if (request is null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                return;
            }

            try
            {
                var success = await passwordRepo.DeletePasswordAsync(passwordId, request.UserId);
                if (success)
                {
                    await context.Response.WriteAsJsonAsync(new { message = "Password entry deleted successfully" });
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new { error = "Password entry not found" });
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = $"Failed to delete password: {ex.Message}" });
            }
        });

        // Search password entries
        app.MapPost("/passwords/search", async (HttpContext context, PasswordRepository passwordRepo) =>
        {
            var request = await JsonSerializer.DeserializeAsync<SearchPasswordsRequest>(context.Request.Body);
            if (request is null || string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid search request" });
                return;
            }

            try
            {
                var results = await passwordRepo.SearchPasswordsAsync(request.UserId, request.SearchTerm);
                
                // Return search results without decrypting passwords for security
                var searchResults = results.Select(p => new SearchResult(
                    p.Id,
                    p.Platform,
                    p.Username,
                    p.Comment,
                    p.CreatedAt,
                    p.UpdatedAt
                )).ToList();

                await context.Response.WriteAsJsonAsync(new { 
                    results = searchResults,
                    totalResults = searchResults.Count,
                    searchTerm = request.SearchTerm
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = $"Search failed: {ex.Message}" });
            }
        });
    }

    // Validation methods
    private static bool IsValidCreateRequest(CreatePasswordRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Platform) &&
               !string.IsNullOrWhiteSpace(request.Username) &&
               !string.IsNullOrWhiteSpace(request.PlainTextPassword) &&
               !string.IsNullOrWhiteSpace(request.MasterPassword) &&
               request.UserId != Guid.Empty;
    }

    private static bool IsValidUpdateRequest(UpdatePasswordRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Platform) &&
               !string.IsNullOrWhiteSpace(request.Username) &&
               !string.IsNullOrWhiteSpace(request.PlainTextPassword) &&
               !string.IsNullOrWhiteSpace(request.MasterPassword) &&
               request.UserId != Guid.Empty;
    }

    // Request/Response models
    private record CreatePasswordRequest(
        string Platform, 
        string Username, 
        string PlainTextPassword, 
        string? Comment, 
        Guid UserId,
        string MasterPassword);

    private record UpdatePasswordRequest(
        string Platform, 
        string Username, 
        string PlainTextPassword, 
        string? Comment, 
        Guid UserId,
        string MasterPassword);

    private record VaultAccessRequest(Guid UserId, string MasterPassword);
    
    private record DeletePasswordRequest(Guid UserId);
    
    private record SearchPasswordsRequest(Guid UserId, string SearchTerm);

    // Response models
    public record VaultEntry(
        Guid Id,
        string Platform,
        string Username,
        string Password,  // Decrypted password for display
        string Comment,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public record SearchResult(
        Guid Id,
        string Platform,
        string Username,
        string Comment,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}