using Api.Middleware;
using Api.Modules;
using Api.Telemetry;
using Application;
using Infrastructure;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Logging;

// Hard cap on the largest possible upload across the whole API. Per-endpoint
// limits should be set with [RequestSizeLimit] / [RequestFormLimits] for tighter
// enforcement; this only catches calls that bypass attribute-level limits.
const int maxRequestBodySizeMb = 30;
var builder = WebApplication.CreateBuilder(args);
builder.Services.SetupServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddApplicationInsightsTelemetryProcessor<PiiTelemetryProcessor>();
builder.Services.AddSingleton<ITelemetryInitializer, ReleaseTagInitializer>();
builder.Services.AddSwaggerGen();
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodySizeMb * 1024 * 1024;
});
builder.Services.Configure<FormOptions>(x =>
{
    x.MultipartBodyLengthLimit = maxRequestBodySizeMb * 1024 * 1024;
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.InitializeDb();
app.UseValidationExceptionHandler();
app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMfaRequired();
app.UseRequestBodyRehydration();
app.MapControllers();
app.MapApiEndpoints();
app.ConfigureHangfire();
app.ConfigureSignalR();
app.ConfigureRequestStagingCleanup();

app.Run();

public partial class Program
{
}