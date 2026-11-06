using Hospital.Infrastructure.Data;
using Hospital.Web.Configuration;
using Hospital.Web.Middleware;
using Hospital.Web.ViewModels.MedicalServices;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;
using System.Text.Json.Serialization;

var healthJsonOptions = new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(port, out var configuredPort) && configuredPort is > 0 and <= 65535)
{
    builder.WebHost.UseUrls($"http://127.0.0.1:{configuredPort}");
}
else if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://127.0.0.1:0");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
builder.Services.AddHospitalServices(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "public,max-age=86400"
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler("/error");

app.MapGet("/media/responsive/{width:int}.webp", async (
    int width,
    string source,
    IWebHostEnvironment environment,
    HttpContext context) =>
{
    if (!MedicalServiceImages.IsSupportedWidth(width) || !MedicalServiceImages.IsLocalImagePath(source))
    {
        return Results.NotFound();
    }

    var file = environment.WebRootFileProvider.GetFileInfo(source.TrimStart('/'));
    if (!file.Exists)
    {
        return Results.NotFound();
    }

    await using var input = file.CreateReadStream();
    using var image = await Image.LoadAsync(input, context.RequestAborted);
    image.Mutate(operation => operation.Resize(new ResizeOptions
    {
        Mode = ResizeMode.Max,
        Size = new Size(width, 0)
    }));

    context.Response.ContentType = "image/webp";
    context.Response.Headers.CacheControl = "public,max-age=86400";
    await image.SaveAsWebpAsync(context.Response.Body, context.RequestAborted);
    return Results.Empty;
}).AllowAnonymous();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy",
            traceId = report.Status == HealthStatus.Healthy ? null : context.TraceIdentifier
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, healthJsonOptions));
    }
}).AllowAnonymous();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await DatabaseInitializer.InitializeAsync(app.Services, app.Environment.IsProduction(), app.Configuration);
app.Run();

public partial class Program
{
}
