using BespokeDuaApi.Auth;
using BespokeDuaApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Bind to Fly's PORT (default 8080) on 0.0.0.0
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "http://192.168.1.40:4200",
            "https://bespoke-dua-client.vercel.app",
            "https://www.bespokedua.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddOpenApi();

builder.Services.AddDbContext<BespokeDuaApi.Data.BespokeDuaDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddScoped<UsageService>();
builder.Services.AddScoped<AppUserService>();
builder.Services.AddScoped<SupabaseAuthAdminService>();
builder.Services.AddScoped<UmmahApiService>();
builder.Services.AddSingleton<ApnsPushService>();
builder.Services.AddHttpClient(nameof(ApnsPushService))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        EnableMultipleHttp2Connections = true
    });
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

var supabaseUrl = app.Configuration["Supabase:Url"];
var supabaseServiceKey = app.Configuration["Supabase:ServiceRoleKey"];
if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(supabaseServiceKey) &&
    !SupabaseConfigHelper.KeysMatchProject(supabaseUrl, supabaseServiceKey))
{
    app.Logger.LogWarning(
        "Supabase ServiceRoleKey is for project {KeyRef} but Supabase:Url is {UrlRef}. Account deletion will fail until they match.",
        SupabaseConfigHelper.GetProjectRefFromJwt(supabaseServiceKey),
        SupabaseConfigHelper.GetProjectRefFromUrl(supabaseUrl));
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
// No UseHttpsRedirection: Kestrel listens on HTTP only (see UseUrls above). In Development there is no
// HTTPS port, which triggers HttpsRedirectionMiddleware's "Failed to determine the https port" warning
// and can break API clients. On Fly, TLS terminates at the edge; traffic to the process stays HTTP.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();