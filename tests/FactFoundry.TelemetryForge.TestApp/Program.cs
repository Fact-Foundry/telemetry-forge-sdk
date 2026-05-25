var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
app.MapStaticAssets();
app.UseAntiforgery();
app.MapRazorComponents<FactFoundry.TelemetryForge.TestApp.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
