using IdentityService.Api.Services;
using IdentityService.Api.Services.Impl;
using IdentityService.Api.Data;
using IdentityService.Api.Repositories;
using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using IdentityService.Api.ExceptionHandling;
using IdentityService.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;
using IdentityService.Api.Messaging;
using IdentityService.Api.Security;
using IdentityService.Api.Hub;
using IdentityService.Api.Hub.Impl;
using Microsoft.AspNetCore.SignalR;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
    options.UseUtcTimestamp = true;
});
builder.Logging.Configure(options =>
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId);

// Controllers / Swagger

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                //Khi serializer enum thì hay convert sang số nguyên, nhưng vì muốn convert sang string cho đúng nghĩa thì nên 
                allowIntegerValues: false)));

var corsAllowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out _))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray()
    ?? [];
if (corsAllowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one absolute origin.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSwaggerGen(options =>
  {
      options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
      {
          Type = SecuritySchemeType.Http,
          Scheme = "bearer",
          BearerFormat = "JWT"
      });

      options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
      {
          [new OpenApiSecuritySchemeReference("Bearer", document)] = []
      });
  });
builder.Services.AddProblemDetails(); // chuẩn json cho error đỡ phải config
builder.Services.AddHttpContextAccessor();
var signalR = builder.Services.AddSignalR();
var signalRRedisConnection = builder.Configuration["SignalR:Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(signalRRedisConnection))
{
    signalR.AddStackExchangeRedis(signalRRedisConnection);
}

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();
// Database
var connectionString =
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Connection string 'Database' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>(); //add trainsient là gì? khi nào xài? tại sao lại xài scope mà ko phải transient.
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ICalendarRepository, CalendarRepository>();
builder.Services.AddScoped<IRbacRepository, RbacRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IRbacService, RbacService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
builder.Services.AddSingleton<IHub, SignalRHub>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<INotificationDeliveryService, NotificationDeliveryService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
    new ProducerBuilder<string, string>(new ProducerConfig
    {
        BootstrapServers = KafkaConfiguration.BootstrapServers(builder.Configuration),
        EnableIdempotence = true
    }).Build());
builder.Services.AddHostedService<ReminderSchedulerWorker>();
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<KafkaNotificationConsumerWorker>();

builder.Services.AddTransient<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

builder.Services.AddScoped<
    IJwtTokenService,
    JwtTokenService>();
builder.Services.AddSingleton<RefreshTokenService>();


var jwtIssuer =
    builder.Configuration["Jwt:Issuer"] //đăng ký bên phát hành (lấy string từ config ra)
    ?? throw new InvalidOperationException(
        "JWT issuer missing.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"] //đăng ký bên nhận (lấy string từ config ra)
    ?? throw new InvalidOperationException(
        "JWT audience missing.");

var jwtKey =
    builder.Configuration["Jwt:Key"] //key để hash token (lấy string từ config ra) sha256, key phải dài ít nhất 32 bytes nếu dưới có nghĩa là không phải hoặc ko có

    ?? throw new InvalidOperationException(
        "JWT key missing.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT key must be at least 32 bytes.");
}

if (builder.Configuration.GetValue<int>(
        "Jwt:AccessTokenMinutes") <= 0 ||
    builder.Configuration.GetValue<int>(
        "Jwt:RefreshTokenDays") <= 0)
{
    throw new InvalidOperationException(
        "JWT token lifetimes must be positive.");
}

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,

                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                NameClaimType = "sub",
                RoleClaimType = "role",
                ClockSkew = TimeSpan.Zero //khoản sai thời gian khi validate exp của token, ở đây TimeSpan.Zero là khoản sai =0 khi exp vừa hết date sẽ lập tức bị cho là hết date nếu khoản sai dài hơn thì sẽ dựa vào đó mà châm chước.
            };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services
    .AddAuthorizationBuilder()
    .SetFallbackPolicy(
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

// Build
var app = builder.Build();

await RbacBootstrapper.SeedAsync(app.Services);

// Middleware
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();
app.UseStatusCodePages();
// Có thể tạm comment khi local chỉ chạy HTTP
// app.UseHttpsRedirection();

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();

app.Run();
