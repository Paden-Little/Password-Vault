var builder = WebApplication.CreateBuilder(args);

//Expose to outside the container
builder.WebHost.UseUrls("http://0.0.0.0:80");

var app = builder.Build();

app.MapGet("/", () => "Hello from .NET API!");

app.Run();