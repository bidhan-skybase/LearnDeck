using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BookMart.Models;
using BookMart.Areas.Identity.Data;
using BookMart.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") 
                       ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

// Change UseSqlServer to UseNpgsql
builder.Services.AddDbContext<ApplicationDbContext>(options => 
    options.UseNpgsql(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await ContextSeed.SeedRolesAsync(userManager, roleManager);
        await ContextSeed.SeedSuperAdminAsync(userManager, roleManager);
        Console.WriteLine("Created Users");
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets(); // Note: MapStaticAssets might be a typo or custom extension; ensure it's correct
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=List}/{id?}")
    .WithStaticAssets(); // WithStaticAssets might be a typo or custom; verify its usage
app.MapRazorPages();
app.Run();