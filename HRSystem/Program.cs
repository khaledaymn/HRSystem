#region Usings

using Hangfire;
using Hangfire.SqlServer;
using HRSystem;
using HRSystem.BackgroundJobs;
using HRSystem.DataBase;
using HRSystem.Extend;
using HRSystem.Filters;
using HRSystem.Repository;
using HRSystem.Services.AttendanceServices;
using HRSystem.Services.EmailServices;
using HRSystem.Services.GeneralSettings;
using HRSystem.Services.UsersServices;
using HRSystem.Settings;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

#endregion


#region Configiration Services


#region Web Aplication

var builder = WebApplication.CreateBuilder(args);

#endregion


#region Logging

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information() 
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
        );
    //.WriteTo.File(
    //    path: "logs/app-log-.txt",
    //    rollingInterval: RollingInterval.Day,
    //    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
    //);
});

builder.Services.AddLogging();

#endregion


#region Connection String Configration

var connectionString = builder.Configuration.GetConnectionString(name: "DefaultConnection") ??
                throw new InvalidOperationException(message: "No connection string was found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseLazyLoadingProxies().UseSqlServer(connectionString));

#endregion


#region Hangfire Configration

builder.Services.AddHangfire(config =>
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

builder.Services.AddHangfireServer();

#endregion


#region API Configration

// Add services to the container.
builder.Services.AddControllers();

#endregion


#region identity Configration

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.AllowedUserNameCharacters = null;
    options.User.RequireUniqueEmail = true;
})
 .AddEntityFrameworkStores<ApplicationDbContext>()
 .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>(TokenOptions.DefaultProvider)
 .AddUserValidator<ArabicUsernameValidator<ApplicationUser>>();

#endregion


#region CORS Configration

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins(
            "http://localhost:4200", 
            "https://balsm-t6vs.vercel.app",
            "https://balsm-demo.vercel.app")
               .AllowAnyMethod()
               .AllowCredentials()
               .AllowAnyHeader();
    });
});

#endregion


#region Authentication Configration

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
        ClockSkew = TimeSpan.Zero
    };
});

#endregion


#region Get Section Configration

#region JWT Configration

builder.Services.Configure<JWT>(builder.Configuration.GetSection(nameof(JWT)));

#endregion

#region Admin Configration

builder.Services.Configure<AdminLogin>(builder.Configuration.GetSection(nameof(AdminLogin)));

#endregion

#region Email Configration

builder.Services.Configure<EmailConfiguration>(builder.Configuration.GetSection(nameof(EmailConfiguration)));

#endregion

#endregion


#region Dependency Injection Configration

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<HangfireJobScheduler>();

#endregion


#region OpenAPI Configration
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

#endregion


#region Swagger Configration
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "HR System API",
        Description = "API documentation for HR System",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Khaled Ayman",
            Email = "khaled654ayman0@gmail.com"
        }
        //License = new OpenApiLicense
        //{
        //    Name = "Not Found any license",
        //    Url = new Uri("https://example.com/license")
        //}
    });

    // JWT Authentication setup
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // Enable XML comments if available
    //var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    //option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    //// Make schema properties optional if they have null values
    //option.SchemaFilter<CustomSchemaFilter>();
});
#endregion


#region Build

var app = builder.Build();

#endregion


#endregion


#region Check Environment
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

#endregion


#region Meddleware

#region Swagger Meddleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var prefix = string.IsNullOrEmpty(options.RoutePrefix) ? "." : "..";
    options.SwaggerEndpoint($"{prefix}/swagger/v1/swagger.json", "HR System API v1");
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    options.DisplayRequestDuration();
});
#endregion

#region Cors Meddelwere
app.UseCors();
#endregion

#region HTTPS Meddleware
app.UseHttpsRedirection();
#endregion

#region Logging

app.UseSerilogRequestLogging();

#endregion

#region Custom Exception Meddelwere

app.UseMiddleware<ExceptionMiddleware>();

#endregion

#region Authentication and Authorization Meddelwere
app.UseAuthentication();
app.UseAuthorization();
#endregion

#region Hangfire

app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = new[]
    {
        new BasicAuthAuthorizationFilter(
            app.Configuration.GetValue<string>("Hangfire:Dashboard:UserName"),
            app.Configuration.GetValue<string>("Hangfire:Dashboard:Password"))
    },
    DashboardTitle = "HR System Dashboard"
});

using (var scope = app.Services.CreateScope())
{
    var scheduler = scope.ServiceProvider.GetRequiredService<HangfireJobScheduler>();
    scheduler.ScheduleRecurringJobs();
}

#endregion

#region Endpoints
app.MapControllers();

app.Run();

#endregion

#endregion