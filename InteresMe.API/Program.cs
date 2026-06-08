using System.Text;
using InteresMe.API.BuildingBlocks.Email;
using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Configuration;
using InteresMe.API.Data;
using InteresMe.API.Modules.Auth.Options;
using InteresMe.API.Modules.Auth.Services;
using InteresMe.API.Modules.Interests.Services;
using InteresMe.API.Modules.Profile.Services;
using InteresMe.API.Modules.Verification.Services;
using InteresMe.API.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

EnvLoader.LoadDotEnv();
AppSecrets.Load();

var builder = WebApplication.CreateBuilder(args);

var frontendOrigins = (Environment.GetEnvironmentVariable("FRONTEND_ORIGINS") ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<AuthFrontendOptions>(
    builder.Configuration.GetSection(AuthFrontendOptions.SectionName));
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(frontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var connectionString = DatabaseConnection.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOAuthRedirectService, OAuthRedirectService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IInterestService, InterestService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<EmailTemplateLoader>();
builder.Services.AddSingleton<EmailTemplateRenderer>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddHttpClient<
    IOAuthProviderService,
    OAuthProviderService>();


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = AppSecrets.JwtIssuer,
            ValidAudience = AppSecrets.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(AppSecrets.AuthTokenSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

var webRootPath = app.Environment.WebRootPath;
if (string.IsNullOrWhiteSpace(webRootPath))
{
    webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
}

Directory.CreateDirectory(webRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath)
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
