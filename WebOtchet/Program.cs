using System.Text.Json.Serialization;
using MySqlConnector;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using WebOtchet.Data;
using WebOtchet.Models;
using WebOtchet.Repositories.IRepository;
using WebOtchet.Repositories.Repository;
using WebOtchet.Services;
using WebOtchet.Infrastructure;
using Doka.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore;
using WebOtchet.Services.Helpers;

var builder = WebApplication.CreateBuilder(args);
var localHttp = builder.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("Hosting:LocalHttp");

var keyPath = builder.Configuration["Hosting:DataProtectionPath"];
if (!string.IsNullOrWhiteSpace(keyPath))
    builder.Services.AddDataProtection()
        .SetApplicationName("Motiv")
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath));


builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddOpenApi();

var connectionString =
    builder.Configuration.GetConnectionString("MariaDb")
    ?? throw new InvalidOperationException(
        "Строка подключения MariaDB не найдена."
    );
var serverVersion =
    MySqlServerVersion.MariaDb(new Version(10, 1, 48), MySqlServerVersionCompatibilityMode.AllowUnsupported);

builder.Services.AddDbContext<WebotchetDbContext>(options =>
    options.UseMySql(new MySqlConnectionStringBuilder(connectionString) { ConvertZeroDateTime = true }.ConnectionString, serverVersion)
);

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme
    )
    .AddCookie(options =>
    {
        options.Cookie.Name = "WebOtchet.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Cookie.SecurePolicy =
            localHttp
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            var idValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var user = Guid.TryParse(idValue, out var id)
                ? await users.GetByIdWithHashAsync(id, context.HttpContext.RequestAborted)
                : null;

            if (user is null || principal?.FindFirstValue(ClaimTypes.Role) != user.Role.ToString() ||
                principal.FindFirstValue(ClaimTypes.Name) != user.Username ||
                principal.FindFirstValue(SessionStamp.ClaimType) != SessionStamp.FromHash(user.Hash))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<
    IIncidentRepository,
    IncidentRepository
>();

builder.Services.AddScoped<
    IUserRepository,
    UserRepository
>();
builder.Services.AddScoped<IncidentService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>
>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
if (!localHttp) app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Configuration.GetValue<bool>("Bootstrap:Initialize"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<WebotchetDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Set<User>().AnyAsync(user => user.Role == Roles.Admin))
    {
        var username = app.Configuration["Bootstrap:Username"] ?? "admin";
        var password = app.Configuration["Bootstrap:Password"];
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Set Bootstrap__Password to create the first administrator.");

        var users = scope.ServiceProvider.GetRequiredService<UserService>();
        await users.CreateAsync(username, password, Roles.Admin);
    }
}

app.Run();

