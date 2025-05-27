using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreMvcTemplate.ViewModels;
using AspNetCoreMvcTemplate.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreMvcTemplate.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var viewModels = new List<UserViewModel>();
            
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                viewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Name = user.FullName,
                    Email = user.Email,
                    Roles = roles.ToList(),
                    SelectedRoles = roles.ToList()
                });
            }
            
            return View(viewModels);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View(new UserViewModel());
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { 
                    UserName = model.Email, 
                    Email = model.Email,
                    FullName = model.Name 
                };
                var result = await _userManager.CreateAsync(user, "DefaultPassword123!"); 

                if (result.Succeeded)
                {
                    await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    return RedirectToAction(nameof(Index));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            return View(new UserViewModel
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Roles = roles.ToList(),
                SelectedRoles = roles.ToList()
            });
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // POST: Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
