using Ghayal_Bhaag.Areas.Identity.Data;
using Ghayal_Bhaag.Models;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ghayal_Bhaag.Controllers
{
    public class UserController: Controller
    {
        private readonly ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context,
                              UserManager<ApplicationUser> userManager,
                              IUserStore<ApplicationUser> userStore,
                              ILogger<UserController> logger,
                              RoleManager<IdentityRole> roleManager
                              )
        {
            _roleManager = roleManager;
            _context = context;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();

            _logger = logger;
        }
        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }

        public async Task<IActionResult> Index()
        {
            var users = _context.Users.ToList();

            foreach (ApplicationUser user in users)
            {
                user.Roles = await GetUserRoles(user);
            }
            return View(users);
        }


        public IActionResult Create()
        {

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> CreateAsync(CreateUserInputModel model)
        {
            if (ModelState.IsValid)
            {
                var checkUser = await _userManager.FindByNameAsync(model.Email);

                if (checkUser != null)
                {
                    ModelState.AddModelError("CustomError", "User already exists with that email.");
                    return View(model);
                }

                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, model.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, model.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    _logger.LogInformation("UserCreationFailed");
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("CustomError", error.Description);
                    }
                    return View(model);
                }
                else { return RedirectToAction("Index"); }
            }

            return View();
        }


        public IActionResult Edit(string id)
        {

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return View("Error");
            }

            var editUser = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Password = string.Empty
            };

            return View(editUser);
        }

        public async Task<IActionResult> Delete(string id)
        {

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return BadRequest();
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest();
            }


            return RedirectToAction("Index");
        }

        public async Task<IActionResult> OnPostAsync(EditUserViewModel user)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser applicationUser = await _userManager.FindByIdAsync(user.Id);


                ApplicationUser otherUser = await _userManager.FindByEmailAsync(user.Email);

                // If the other user has a different ID then the email is already taken.
                if (otherUser != null && otherUser.Id != applicationUser.Id)
                {
                    ModelState.AddModelError("CustomError", "This Email already taken");
                    return View("Edit", user);
                }

                applicationUser.Email = user.Email;
                applicationUser.PasswordHash = _userManager.PasswordHasher.HashPassword(applicationUser, user.Password);

                var result = await _userManager.UpdateAsync(applicationUser);
                return RedirectToAction("Index");
            }

            return RedirectToAction("Edit", new { id = user.Id });
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
