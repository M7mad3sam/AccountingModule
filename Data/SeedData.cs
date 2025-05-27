using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspNetCoreMvcTemplate.Models;

namespace AspNetCoreMvcTemplate.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            // Create roles if they don't exist
            string[] roleNames = { "Admin", "Accountant", "Auditor", "Manager" };
            foreach (var roleName in roleNames)
            {
                if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
                {
                    roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
                }
            }

            // Create initial admin user if none exists
            if (!userManager.Users.Any())
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@application.com",
                    Email = "admin@application.com",
                    EmailConfirmed = true,
                    FullName = "Administrator",
                    DateOfBirth = DateTime.Today
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRolesAsync(adminUser, new[] { "Admin", "Manager" });
                }
            }
            else
            {
                // Ensure at least one admin exists
                var existingAdmins = await userManager.GetUsersInRoleAsync("Admin");
                if (!existingAdmins.Any())
                {
                    var firstUser = await userManager.Users.FirstOrDefaultAsync();
                    if (firstUser != null)
                    {
                        await userManager.AddToRoleAsync(firstUser, "Admin");
                    }
                }
            }
        }
    }
}
