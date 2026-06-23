using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Port=3306;Database=libwise;User=root;Password=;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("LibrarianOrAbove", policy => policy.RequireRole("Admin", "Librarian"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Admin", "Librarian", "AssistantLibrarian"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
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
            UserName = "admin",
            Email = "admin@libwise.com",
            FirstName = "System",
            LastName = "Admin",
            Role = "Admin",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, "Admin123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    if (await userManager.FindByEmailAsync("librarian@libwise.com") == null)
    {
        var librarian = new ApplicationUser
        {
            UserName = "librarian",
            Email = "librarian@libwise.com",
            FirstName = "Jane",
            LastName = "Librarian",
            Role = "Librarian",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(librarian, "Lib123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(librarian, "Librarian");
    }

    if (await userManager.FindByEmailAsync("al@libwise.com") == null)
    {
        var al = new ApplicationUser
        {
            UserName = "al",
            Email = "al@libwise.com",
            FirstName = "John",
            LastName = "Assistant",
            Role = "AssistantLibrarian",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(al, "Al123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(al, "AssistantLibrarian");
    }

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
}

app.Run();
