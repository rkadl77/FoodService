using hitsApplication.AuthServices;
using hitsApplication.Data;
using hitsApplication.Filters;
using hitsApplication.Models;
using hitsApplication.Services; 
using hitsApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using hitsApplication.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BugCaseLoggingSettings>(
    builder.Configuration.GetSection("BugCaseLogging"));
builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection("FeatureFlags"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HitsApplication API",
        Version = "v1",
        Description = "API for cart management with guest access"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.OperationFilter<BasketIdOperationFilter>();
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "HitsApplication.Cart";
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<BuggyFeaturesService>();
builder.Services.AddScoped<RequireAuthorizationAttribute>();


builder.Services.AddSingleton<IBugCaseLoggingService, FileBugCaseLoggingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HitsApplication v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseRouting();

app.UseMiddleware<BugCaseLoggingMiddleware>();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }