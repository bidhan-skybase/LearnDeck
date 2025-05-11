using BookMart.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;


public static class ContextSeed
{
    public static async Task SeedRolesAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        //Seed Roles
        await roleManager.CreateAsync(new IdentityRole(Roles.User.ToString()));
        await roleManager.CreateAsync(new IdentityRole(Roles.Member.ToString()));
        await roleManager.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
    }
    public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        //Seed Default User
        var defaultUser = new ApplicationUser
        {
            UserName = "admin",
            FirstName = "Ghayal",
            LastName = "Bhaag",
            Email = "admin@gmail.com",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            LockoutEnabled = false
        };
        if (userManager.Users.All(u => u.Id != defaultUser.Id))
        {
            var user = await userManager.FindByEmailAsync(defaultUser.Email);
            if (user == null)
            {
                await userManager.CreateAsync(defaultUser, "Admin!123");
                await userManager.AddToRoleAsync(defaultUser, Roles.User.ToString());
                await userManager.AddToRoleAsync(defaultUser, Roles.Member.ToString());
                await userManager.AddToRoleAsync(defaultUser, Roles.Admin.ToString());
            }

        }
    }
}
