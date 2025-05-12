using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BookMart.Areas.Identity.Data;
using BookMart.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BookMart.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserRolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> ManageRoles(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User ID is required.";
                return RedirectToAction("Index", "User");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"User with ID {userId} cannot be found.";
                return View("NotFound");
            }

            ViewBag.userId = userId;
            ViewBag.UserName = user.UserName;

            var model = new List<ManageUserRolesViewModel>();
            var roles = await _roleManager.Roles.ToListAsync();
            foreach (var role in roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    Selected = await _userManager.IsInRoleAsync(user, role.Name)
                };
                model.Add(userRolesViewModel);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(List<ManageUserRolesViewModel> model, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User ID is required.";
                return RedirectToAction("Index", "User");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"User with ID {userId} cannot be found.";
                return View("NotFound");
            }

            // Check if user is an admin
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var adminRoleSelected = model.Any(x => x.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) && x.Selected);

            // Prevent removing Admin role from an admin user
            if (isAdmin && !adminRoleSelected)
            {
                TempData["ErrorMessage"] = $"Cannot remove the Admin role from user '{user.Email}'.";
                return RedirectToAction("ManageRoles", new { userId });
            }

            // Remove existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to remove existing roles: " + string.Join(", ", removeResult.Errors.Select(e => e.Description));
                return View(model);
            }

            // Add selected roles
            var selectedRoles = model.Where(x => x.Selected).Select(x => x.RoleName).ToList();
            if (selectedRoles.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, selectedRoles);
                if (!addResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Failed to add selected roles: " + string.Join(", ", addResult.Errors.Select(e => e.Description));
                    return View(model);
                }
            }

            TempData["SuccessMessage"] = $"Roles for user '{user.Email}' updated successfully.";
            return RedirectToAction("Index", "User");
        }
    }
}