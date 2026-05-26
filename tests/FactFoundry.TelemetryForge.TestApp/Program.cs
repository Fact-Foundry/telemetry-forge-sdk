using FactFoundry.TelemetryForge.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var tfConfig = builder.Configuration.GetSection("TelemetryForge");
var apiKey = tfConfig["ApiKey"];

if (!string.IsNullOrEmpty(apiKey))
{
    builder.Services.AddTelemetryForge(options =>
    {
        options.Endpoint = tfConfig["Endpoint"] ?? "http://localhost:5090";
        options.ApiKey = apiKey;
    });
}

var app = builder.Build();
app.MapStaticAssets();
app.UseAntiforgery();

if (!string.IsNullOrEmpty(apiKey))
    app.UseTelemetryForge();

app.MapRazorComponents<FactFoundry.TelemetryForge.TestApp.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
