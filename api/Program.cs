using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmwHackTracker.api.DAL;
using dotenv.net;

namespace SmwHackTracker.api
{
    public class Program
    {
        public static async Task Main(string[] args)
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
            builder.Services.AddSingleton<EmailService>();


            var app = builder.Build();

            app.UseCors();

            app.MapGet("/", () => "Hello from .NET API!");

            app.MapAuthEndpoints();
            app.MapPasswordEndpoints();


            EmailService email = new EmailService();
            await email.SendEmailAsync("little.paden@gmail.com", "Subject test", "body test");


            app.Run();
        }
    }
}