using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmwHackTracker.api.DAL;

namespace SmwHackTracker.api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.UseUrls("http://0.0.0.0:80");

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            builder.Services.AddSingleton<DapperContext>();
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<PasswordRepository>();

            var app = builder.Build();

            app.UseCors();

            app.MapGet("/", () => "Hello from .NET API!");

            app.MapAuthEndpoints();
            app.MapPasswordEndpoints();

            app.Run();

        }
    }
}