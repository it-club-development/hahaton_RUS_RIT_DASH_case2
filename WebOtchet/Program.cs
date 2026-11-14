using System.Text.Json.Serialization;
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

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddControllers()
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
    MySqlServerVersion.MariaDb(new Version(12,3,3));

builder.Services.AddDbContext<WebotchetDbContext>(options =>
    options.UseMySql(connectionString, serverVersion)
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
            builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

