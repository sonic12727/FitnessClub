using FitnessClub.Core.Services;
using FitnessClub.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Добавляем конфигурацию в БД
builder.Services.AddDbContext<FitnessClubDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Все мои сервисы
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<JwtService>();

// Все мои контроллеры
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// JWT-сервис для аутентификации
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
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])
        ),
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();

// Миддлвейры
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("/admin/{*path}", "admin/dashboard.html");
app.MapFallbackToFile("/client/{*path}", "client/profile.html");
app.MapFallbackToFile("index.html");

// Тестовые данные
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FitnessClubDbContext>();
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();

    context.Database.EnsureCreated();

    if (!context.Users.Any())
    {
        // Админ: логика создания
        var admin = new FitnessClub.Core.Entities.User
        {
            FirstName = "Админ",
            LastName = "Системы",
            Email = "admin@fitness.ru",
            Phone = "+79991112233",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = FitnessClub.Core.Enums.UserRole.Admin,
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        context.Users.Add(admin);

        // Клиент: логика создания
        for (int i = 1; i <= 5; i++)
        {
            var client = new FitnessClub.Core.Entities.User
            {
                FirstName = $"Клиент{i}",
                LastName = $"Фамилия{i}",
                Email = $"client{i}@mail.ru",
                Phone = $"+7999{1000000 + i}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword($"client{i}"),
                Role = FitnessClub.Core.Enums.UserRole.Client,
                CreatedAt = DateTime.Now,
                IsActive = true,
                Membership = new FitnessClub.Core.Entities.Membership
                {
                    Type = i % 2 == 0 ? FitnessClub.Core.Enums.MembershipType.Monthly : FitnessClub.Core.Enums.MembershipType.OneTime,
                    StartDate = DateTime.Now.AddDays(-i),
                    EndDate = DateTime.Now.AddMonths(i),
                    Price = i * 1000,
                    IsActive = true,
                    RemainingVisits = i % 2 == 0 ? 0 : 1
                }
            };
            context.Users.Add(client);
        }
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Тестовые данные созданы");
    }
}
app.Run();