using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InteresMe.API.BuildingBlocks.Email;
using InteresMe.API.BuildingBlocks.Errors;
using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Configuration;
using InteresMe.API.Data;
using InteresMe.API.Modules.Auth.Options;
using InteresMe.API.Modules.Auth.Services;
using InteresMe.API.Modules.Chat.Channels.Services;
using InteresMe.API.Modules.Chat.DirectMessages.Services;
using InteresMe.API.Modules.Chat.Groups.Services;
using InteresMe.API.Modules.Discovery.Services;
using InteresMe.API.Modules.History.Services;
using InteresMe.API.Modules.Initiatives.Services.InitiativeManagement;
using InteresMe.API.Modules.Initiatives.Services.InitiativeQueries;
using InteresMe.API.Modules.Initiatives.Services.JoinRequests;
using InteresMe.API.Modules.Initiatives.Services.Slugs;
using InteresMe.API.Modules.Initiatives.Validators;
using InteresMe.API.Modules.Interests.Services;
using InteresMe.API.Modules.Posts.Comments.Services;
using InteresMe.API.Modules.Posts.Feed.Services;
using InteresMe.API.Modules.Posts.Likes.Services;
using InteresMe.API.Modules.Posts.Shares.Services;
using InteresMe.API.Modules.Profile.Services;
using InteresMe.API.Modules.Verification.Services;
using InteresMe.API.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

EnvLoader.LoadDotEnv();
AppSecrets.Load();

var builder = WebApplication.CreateBuilder(args);

var frontendOrigins = (Environment.GetEnvironmentVariable("FRONTEND_ORIGINS") ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key,
                    entry => entry.Value!.Errors
                        .Select(_ => "The supplied value is invalid.")
                        .ToArray());

            return new BadRequestObjectResult(new ApiErrorResponse
            {
                Code = ApiErrorCodes.ValidationFailed,
                Message = "Request validation failed.",
                Status = StatusCodes.Status400BadRequest,
                TraceId = context.HttpContext.TraceIdentifier,
                Errors = errors
            });
        };
    });
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
builder.Services.AddScoped<IProfileViewService, ProfileViewService>();
builder.Services.AddScoped<IProfileRequestReader, ProfileRequestReader>();
builder.Services.AddScoped<IInterestService, InterestService>();
builder.Services.AddScoped<IInterestCatalogService, InterestCatalogService>();
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddScoped<IUserHistoryService, UserHistoryService>();
builder.Services.AddScoped<IPostFeedService, PostFeedService>();
builder.Services.AddScoped<IPostCreateRequestReader, PostCreateRequestReader>();
builder.Services.AddScoped<IPostAttachmentValidator, PostAttachmentValidator>();
builder.Services.AddScoped<IPostAttachmentStorage, LocalPostAttachmentStorage>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPostLikeService, PostLikeService>();
builder.Services.AddScoped<IPostShareService, PostShareService>();
builder.Services.AddScoped<IDirectMessageService, DirectMessageService>();
builder.Services.AddScoped<IGroupChatService, GroupChatService>();
builder.Services.AddScoped<IChannelService, ChannelService>();
builder.Services.AddScoped<IInitiativeQueryService, InitiativeQueryService>();
builder.Services.AddScoped<IInitiativeManagementService, InitiativeManagementService>();
builder.Services.AddScoped<IInitiativeJoinRequestService, InitiativeJoinRequestService>();
builder.Services.AddScoped<IInitiativeSlugService, InitiativeSlugService>();
builder.Services.AddScoped<IInitiativeRequestValidator, InitiativeRequestValidator>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IVerificationTokenService, VerificationTokenService>();
builder.Services.AddScoped<IPhoneNumberNormalizer, PhoneNumberNormalizer>();
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

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var subject =
                    context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                    context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(subject, out _))
                {
                    context.Fail("The access token subject is invalid.");
                }

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Bearer";

                await context.Response.WriteAsJsonAsync(
                    new ApiErrorResponse
                    {
                        Code = ApiErrorCodes.Unauthorized,
                        Message = "Authentication is required.",
                        Status = StatusCodes.Status401Unauthorized,
                        TraceId = context.HttpContext.TraceIdentifier
                    },
                    context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(
                    new ApiErrorResponse
                    {
                        Code = ApiErrorCodes.Forbidden,
                        Message = "You do not have permission to perform this action.",
                        Status = StatusCodes.Status403Forbidden,
                        TraceId = context.HttpContext.TraceIdentifier
                    },
                    context.HttpContext.RequestAborted);
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return Task.CompletedTask;
    });

    await next();
});

await app.ApplyMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    await app.SeedDevelopmentDemoDataAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");
app.UseMiddleware<ExceptionHandlingMiddleware>();

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
