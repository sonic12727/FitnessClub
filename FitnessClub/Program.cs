using FitnessClub.Core.Services;
using FitnessClub.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL
builder.Services.AddDbContext<FitnessClubDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Сервисы
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<StatisticsService>();

// Контроллеры
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)
        ),
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();

// Middleware
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("/client/{*path}", "client/profile.html");
app.MapFallbackToFile("index.html");

// Инициализация БД + сидирование
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FitnessClubDbContext>();

    // Для PostgreSQL + EF Core migrations
    context.Database.Migrate();

    if (!context.Users.Any())
    {
        var now = DateTime.UtcNow;

        var admin = new FitnessClub.Core.Entities.User
        {
            FirstName = "Админ",
            LastName = "Системы",
            Email = "admin@fitness.ru",
            Phone = "+79991112233",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = FitnessClub.Core.Enums.UserRole.Admin,
            CreatedAt = now,
            IsActive = true
        };

        context.Users.Add(admin);

        await context.SaveChangesAsync();

        Console.WriteLine("✅ Тестовые данные созданы в PostgreSQL");
    }
}

app.Run();