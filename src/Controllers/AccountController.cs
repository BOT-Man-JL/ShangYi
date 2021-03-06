﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShangYi.Models;
using ShangYi.Models.AccountViewModels;
using System.Threading.Tasks;

namespace ShangYi.Controllers
{
	[Authorize]
	public class AccountController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly ILogger _logger;

		public AccountController (
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			ILoggerFactory loggerFactory)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_logger = loggerFactory.CreateLogger<AccountController> ();
		}

		// GET: /Account/Login
		[HttpGet]
		[AllowAnonymous]
		public IActionResult Login (string returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			return View ();
		}

		// POST: /Account/Login
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login (LoginViewModel model, string returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			if (ModelState.IsValid)
			{
				// This doesn't count login failures towards account lockout
				// To enable password failures to trigger account lockout,
				// set lockoutOnFailure: true
				var result = await _signInManager.PasswordSignInAsync (
					model.Email, model.Password, model.RememberMe,
					lockoutOnFailure: false);
				if (result.Succeeded)
				{
					_logger.LogInformation (1, "User logged in.");
					return RedirectToLocal (returnUrl);
				}
				if (result.IsLockedOut)
				{
					_logger.LogWarning (2, "User account locked out.");
					return View ("Lockout");
				}
				else
				{
					ModelState.AddModelError (string.Empty, "Invalid login attempt.");
					return View (model);
				}
			}

			// If we got this far, something failed, redisplay form
			return View (model);
		}

		// GET: /Account/Register
		[HttpGet]
		[AllowAnonymous]
		public IActionResult Register (string returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			return View ();
		}

		// POST: /Account/Register
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register (RegisterViewModel model, string returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			if (ModelState.IsValid)
			{
				var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
				var result = await _userManager.CreateAsync (user, model.Password);
				if (result.Succeeded)
				{
					// For more information on how to enable account confirmation and password reset
					// please visit http://go.microsoft.com/fwlink/?LinkID=532713
					// Send an email with this link
					//var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
					//var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code },
					//    protocol: HttpContext.Request.Scheme);
					//await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
					//    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
					await _signInManager.SignInAsync (user, isPersistent: false);
					_logger.LogInformation (3, "User created a new account with password.");
					return RedirectToLocal (returnUrl);
				}
				AddErrors (result);
			}

			// If we got this far, something failed, redisplay the form
			return View (model);
		}

		// POST: /Account/LogOff
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> LogOff ()
		{
			await _signInManager.SignOutAsync ();
			_logger.LogInformation (4, "User logged out.");
			return RedirectToAction (nameof (HomeController.Index), "Home");
		}

		// GET: /Account/ForgotPassword
		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ForgotPassword ()
		{
			if (_signInManager.IsSignedIn (User))
			{
				var user = await _userManager.GetUserAsync (User);
				var userName = await _userManager.GetUserNameAsync (user);
				if (userName == "Admin@admin.com")
					return RedirectToAction (nameof (ResetPassword));
			}
			return View ();
		}

		// GET: /Account/ResetPassword
		[HttpGet]
		[Authorize]
		public IActionResult ResetPassword ()
		{
			return View ();
		}

		// POST: /Account/ResetPassword
		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword (ResetPasswordViewModel model)
		{
			var userAdmin = await _userManager.GetUserAsync (User);
			var userName = await _userManager.GetUserNameAsync (userAdmin);
			if (userName != "Admin@admin.com")
				return NotFound ();

			if (!ModelState.IsValid)
			{
				return View (model);
			}
			var user = await _userManager.FindByNameAsync (model.Email);
			if (user == null)
			{
				// Don't reveal that the user does not exist
				return RedirectToAction (nameof (ForgotPassword));
			}
			var code = await _userManager.GeneratePasswordResetTokenAsync (user);
			// Alt Sln: Send via Email
			// var callbackUrl = Url.Action("ResetPassword", "Account",
			// new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
			var result = await _userManager.ResetPasswordAsync (user, code, model.Password);
			if (result.Succeeded)
			{
				return RedirectToAction (nameof (ResetPasswordDone));
			}
			AddErrors (result);
			return View ();
		}

		// GET: /Account/ResetPasswordConfirmation
		[HttpGet]
		[AllowAnonymous]
		public string ResetPasswordDone ()
		{
			return "Done";
		}

		#region Helpers

		private void AddErrors (IdentityResult result)
		{
			foreach (var error in result.Errors)
				ModelState.AddModelError (string.Empty, error.Description);
		}

		private Task<ApplicationUser> GetCurrentUserAsync ()
		{
			return _userManager.GetUserAsync (HttpContext.User);
		}

		private IActionResult RedirectToLocal (string returnUrl)
		{
			if (Url.IsLocalUrl (returnUrl))
				return Redirect (returnUrl);
			else
				return RedirectToAction (nameof (HomeController.Index), "Home");
		}

		#endregion
	}
}
