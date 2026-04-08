using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер зависимостей
builder.Services.AddControllersWithViews();

// Настройка контекста базы данных
builder.Services.AddDbContext<RepairDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Упрощенные настройки пароля (по вашему желанию)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Настройки пользователя
    options.User.RequireUniqueEmail = true;

    // Настройки блокировки (по умолчанию, можно изменить)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<RepairDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI(); // Добавляет стандартные UI страницы Identity (логин, регистрация и т.д.)

// Настройка Application Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);

    // Для защиты от CSRF
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Настройка сессий (если всё ещё нужны)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Настройка кэширования
builder.Services.AddMemoryCache();

// Регистрация других сервисов (если есть в проекте)
// builder.Services.AddScoped<IService, Service>();

var app = builder.Build();

// Конфигурация конвейера HTTP запросов
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // UseMigrationsEndPoint удален - он несовместим с WebApplication в .NET 8
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ВАЖНО: UseAuthentication ДО UseAuthorization
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Инициализация базы данных с начальными данными
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Применение миграций
        var context = services.GetRequiredService<RepairDbContext>();
        //context.Database.Migrate(); // Применяет все pending миграции

        // Инициализация ролей и администратора
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка при инициализации базы данных");
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<RepairDbContext>();
        //context.Database.Migrate();

        // Вызываем SeedData с логированием
        Console.WriteLine("=== STARTING SEED DATA ===");
        await SeedData.Initialize(services);
        Console.WriteLine("=== SEED DATA COMPLETED ===");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка при инициализации базы данных");
        Console.WriteLine($"SEED DATA ERROR: {ex.Message}");
        Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
    }
}

// Настройка маршрутов для Identity Razor Pages
app.MapRazorPages(); // Это для страниц Identity (логин, регистрация и т.д.)

// Маршрутизация контроллеров - ВАЖНО: используем Dashboard как главную страницу
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();