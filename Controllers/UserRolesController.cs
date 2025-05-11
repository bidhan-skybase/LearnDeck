using BookMart.Areas.Identity.Data;
using BookMart.Models;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace BookMart.Controllers
{
        // to allow all features available for now for GitHub demo, the authorization access is commented
        // [Authorize(Roles = "Manager")]
        public class UserRolesController : Controller
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly RoleManager<IdentityRole> _roleManager;
            public UserRolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
            {
                _roleManager = roleManager;
                _userManager = userManager;
            }
            public async Task<IActionResult> GetRoles()
            {
                var users = await _userManager.Users.ToListAsync();
                var userRolesViewModel = new List<ApplicationUser>();
                foreach (ApplicationUser user in users)
                {
                    var thisViewModel = new ApplicationUser();
                    thisViewModel.Id = user.Id;
                    thisViewModel.Email = user.Email;
                    thisViewModel.FirstName = user.FirstName;
                    thisViewModel.LastName = user.LastName;
                    thisViewModel.Roles = await GetUserRoles(user);
                    userRolesViewModel.Add(thisViewModel);
                }
                return View(userRolesViewModel);
            }
            private async Task<List<string>> GetUserRoles(ApplicationUser user)
            {
                return new List<string>(await _userManager.GetRolesAsync(user));
            }
            public async Task<IActionResult> ManageRoles(string userId)
            {
                ViewBag.userId = userId;
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                    return View("NotFound");
                }
                ViewBag.UserName = user.UserName;
                var model = new List<ManageUserRolesViewModel>();
                foreach (var role in _roleManager.Roles)
                {
                    var userRolesViewModel = new ManageUserRolesViewModel
                    {
                        RoleId = role.Id,
                        RoleName = role.Name
                    };
                    if (await _userManager.IsInRoleAsync(user, role.Name))
                    {
                        userRolesViewModel.Selected = true;
                    }
                    else
                    {
                        userRolesViewModel.Selected = false;
                    }
                    model.Add(userRolesViewModel);
                }
                return View(model);
            }
            [HttpPost]
            public async Task<IActionResult> ManageRoles(List<ManageUserRolesViewModel> model, string userId)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return View();
                }
                var roles = await _userManager.GetRolesAsync(user);
                var result = await _userManager.RemoveFromRolesAsync(user, roles);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Cannot remove user existing roles");
                    return View(model);
                }
                result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Cannot add selected roles to user");
                    return View(model);
                }
                return RedirectToAction("Index", "User");
            }
        }
}
