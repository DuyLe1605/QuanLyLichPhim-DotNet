using BaiTapLon.Api.Services;
using BaiTapLon.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("CineManagerWeb", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<TokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var known = db.Database.GetMigrations().ToList();
        var applied = db.Database.GetAppliedMigrations().ToList();
        var pending = db.Database.GetPendingMigrations().ToList();
        app.Logger.LogInformation("EF migrations: known={KnownCount}, applied={AppliedCount}, pending={PendingCount}", known.Count, applied.Count, pending.Count);
        if (known.Count <= 50)
        {
            app.Logger.LogInformation("Known migrations: {Migrations}", string.Join(", ", known));
        }
        if (pending.Count > 0 && pending.Count <= 50)
        {
            app.Logger.LogInformation("Pending migrations: {Migrations}", string.Join(", ", pending));
        }

        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to apply database migrations on startup.");
    }
}

app.UseCors("CineManagerWeb");

var resourcesPath = Path.Combine(builder.Environment.ContentRootPath, "..", "BaiTapLon", "Resources");
if (Directory.Exists(resourcesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(resourcesPath),
        RequestPath = "/Resources"
    });
}

app.MapControllers();

app.Run();
