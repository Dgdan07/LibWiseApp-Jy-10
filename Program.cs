using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using LibWiseApp.Data;
using LibWiseApp.Middleware;
using LibWiseApp.Models;

static string ToNpgsqlConnectionString(string raw)
{
    if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return raw;
    }

    var uri = new Uri(raw);
    var userInfo = uri.UserInfo.Split(':', 2);
    var npgsqlBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        SslMode = SslMode.Require
    };
    return npgsqlBuilder.ConnectionString;
}

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting LibWiseApp");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllersWithViews(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(rawConnectionString))
    rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(rawConnectionString))
    throw new InvalidOperationException("No connection string found. Set ConnectionStrings:DefaultConnection or DATABASE_URL.");

var connectionString = ToNpgsqlConnectionString(rawConnectionString);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LibWiseApp.Services.AuditLogService>();
builder.Services.AddScoped<LibWiseApp.Services.FineCalculationService>();
builder.Services.AddScoped<LibWiseApp.Services.BorrowingService>();
builder.Services.AddScoped<LibWiseApp.Services.DashboardStatsService>();
builder.Services.AddScoped<LibWiseApp.Services.BookCatalogService>();
builder.Services.AddScoped<LibWiseApp.Services.ReportQueryService>();
builder.Services.AddSingleton<LibWiseApp.Services.BookCoverService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("LibrarianOrAbove", policy => policy.RequireRole("Admin", "Librarian"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Admin", "Librarian", "AssistantLibrarian"));
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<ExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?statusCode={0}");
app.UseStaticFiles();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    // EnsureCreatedAsync is a no-op on a database that already has tables, so it never applies
    // migrations added after the first deploy. Patch newly-added columns here idempotently instead.
    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"Books\" ADD COLUMN IF NOT EXISTS \"CoverImage\" bytea");
    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"Books\" ADD COLUMN IF NOT EXISTS \"CoverImageContentType\" character varying(100)");

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Librarian", "AssistantLibrarian" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    if (await userManager.FindByEmailAsync("admin@libwise.com") == null)
    {
        var admin = new ApplicationUser
        {
            UserName = "John Admin",
            Email = "admin@libwise.com",
            FirstName = "John",
            LastName = "Admin",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, "Admin123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
        else
            Log.Warning("Failed to seed admin user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    if (await userManager.FindByEmailAsync("librarian@libwise.com") == null)
    {
        var librarian = new ApplicationUser
        {
            UserName = "Doe Librarian",
            Email = "librarian@libwise.com",
            FirstName = "Doe",
            LastName = "Librarian",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(librarian, "Librarian123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(librarian, "Librarian");
        else
            Log.Warning("Failed to seed librarian user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    if (await userManager.FindByEmailAsync("al@libwise.com") == null)
    {
        var al = new ApplicationUser
        {
            UserName = "Dan Assit",
            Email = "al@libwise.com",
            FirstName = "Dan",
            LastName = "Assit",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(al, "Assistant123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(al, "AssistantLibrarian");
        else
            Log.Warning("Failed to seed assistant librarian user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    // The blocks above only run for users that don't exist yet, so on an already-seeded
    // database (e.g. Render) they never update names. Patch existing accounts idempotently.
    async Task UpdateSeededNameAsync(string email, string firstName, string lastName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null && (user.FirstName != firstName || user.LastName != lastName))
        {
            user.FirstName = firstName;
            user.LastName = lastName;
            await userManager.UpdateAsync(user);
        }
    }

    await UpdateSeededNameAsync("admin@libwise.com", "John", "Admin");
    await UpdateSeededNameAsync("librarian@libwise.com", "Doe", "Librarian");
    await UpdateSeededNameAsync("al@libwise.com", "Dan", "Assit");

    async Task UpdateSeededUserNameAsync(string email, string userName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null && user.UserName != userName)
            await userManager.SetUserNameAsync(user, userName);
    }

    await UpdateSeededUserNameAsync("admin@libwise.com", "John Admin");
    await UpdateSeededUserNameAsync("librarian@libwise.com", "Doe Librarian");
    await UpdateSeededUserNameAsync("al@libwise.com", "Dan Assit");

    if (!db.Categories.Any())
    {
        db.Categories.AddRange(
            new Category { Name = "Fiction", Description = "Fictional literature" },
            new Category { Name = "Non-Fiction", Description = "Factual and educational" },
            new Category { Name = "Science & Technology", Description = "STEM subjects" },
            new Category { Name = "Mathematics", Description = "Math textbooks and references" },
            new Category { Name = "History", Description = "Historical texts" },
            new Category { Name = "Philosophy", Description = "Philosophical works" },
            new Category { Name = "Reference", Description = "Encyclopedias, dictionaries, etc." }
        );
        db.SaveChanges();
    }

    if (!db.FineRules.Any())
    {
        db.FineRules.Add(new FineRule { DaysAllowed = 14, DailyFineRate = 5.00m, MaxFine = 500.00m });
        db.SaveChanges();
    }

    await DatabaseSeeder.InitializeAsync(db);

    // Runs every startup (not just on first seed) so a database seeded before this
    // consistency logic existed gets patched too, instead of only new databases.
    await DatabaseSeeder.PatchDataConsistencyAsync(db);
}

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "LibWiseApp terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
