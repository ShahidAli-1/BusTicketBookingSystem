using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Models;
using BusTicketBookingSystem.Services;
using BusTicketBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace BusTicketBookingSystem.Controllers
{
    public class AccountController : Controller
    {

        private readonly ApplicationDbContext _db;
        private readonly IPasswordService _pwd;
        private readonly IEmailService _email;      

        public AccountController(
            ApplicationDbContext db,
            IPasswordService pwd,
            IEmailService email)                  
        {
            _db = db;
            _pwd = pwd;
            _email = email;                         
        }
        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (_db.Passengers.Any(p => p.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            var tempPassword = _pwd.GenerateTemporaryPassword();

            var passenger = new Passenger
            {
                FullName = model.FullName,
                Email = model.Email,
                ContactNo = model.ContactNo,
                Gender = model.Gender,
                PasswordHash = _pwd.HashPassword(tempPassword),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.Passengers.Add(passenger);
            await _db.SaveChangesAsync();

            // In production: send temp password via email
            TempData["TempPassword"] = tempPassword;
            TempData["RegisteredEmail"] = model.Email;
            TempData["Message"] = "Registration successful! Your temporary password is shown below. Please note it and login.";

            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            // Check Admin first
            var admin = _db.Admins.FirstOrDefault(a => a.Email == model.Email);
            if (admin != null && _pwd.VerifyPassword(model.Password, admin.PasswordHash))
            {
                await SignInUser(admin.AdminID.ToString(), admin.Email, admin.FullName, "Admin");
                return RedirectToAction("Dashboard", "Admin");
            }

            // Check Passenger
            var passenger = _db.Passengers.FirstOrDefault(p => p.Email == model.Email);
            if (passenger != null && passenger.IsActive &&
                _pwd.VerifyPassword(model.Password, passenger.PasswordHash))
            {
                await SignInUser(passenger.PassengerID.ToString(), passenger.Email, passenger.FullName, "Passenger");
                return RedirectToAction("Dashboard", "Passenger");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUser(string id, string email, string name, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity));
        }

        private IActionResult RedirectToDashboard()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole("Passenger"))
                return RedirectToAction("Dashboard", "Passenger");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ForgotPassword
        
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        
        // POST: /Account/ForgotPassword
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var passenger = await _db.Passengers
                .FirstOrDefaultAsync(p => p.Email == model.Email);

            // Security: don't reveal whether email exists
            if (passenger == null || !passenger.IsActive)
            {
                TempData["Message"] = "If that email is registered, a code has been sent.";
                return RedirectToAction(nameof(VerifyCode), new { email = model.Email });
            }

            // Invalidate any previous unused codes
            var oldCodes = await _db.PasswordResetCodes
                .Where(c => c.PassengerID == passenger.PassengerID && !c.IsUsed)
                .ToListAsync();
            foreach (var c in oldCodes) c.IsUsed = true;

            // Generate a 6-digit code
            var rnd = new Random();
            var code = rnd.Next(100000, 999999).ToString();

            _db.PasswordResetCodes.Add(new PasswordResetCode
            {
                PassengerID = passenger.PassengerID,
                Code = code,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(10),
                IsUsed = false
            });
            await _db.SaveChangesAsync();

            // Send code via email
            var body = $@"
        <div style='font-family:Arial,sans-serif;max-width:500px;margin:auto;padding:20px;'>
            <h2 style='color:#0d6efd;'>🚌 Password Reset Code</h2>
            <p>Hi <strong>{passenger.FullName}</strong>,</p>
            <p>Use this 6-digit code to reset your password:</p>
            <div style='background:#f1f3f5;padding:20px;text-align:center;
                        font-size:30px;letter-spacing:8px;font-weight:bold;
                        border-radius:8px;'>
                {code}
            </div>
            <p style='color:#888;font-size:12px;margin-top:20px;'>
                This code expires in <strong>10 minutes</strong>.
                If you didn't request this, ignore this email.
            </p>
        </div>";

            try
            {
                await _email.SendAsync(passenger.Email, "Your Password Reset Code", body);
                TempData["Message"] = "A 6-digit code has been sent to your email.";
            }
            catch
            {
                TempData["Error"] = "Failed to send email. Please try again later.";
            }

            return RedirectToAction(nameof(VerifyCode), new { email = model.Email });
        }

      
        // GET: /Account/VerifyCode?email=...
        
        [HttpGet]
        public IActionResult VerifyCode(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(ForgotPassword));

            return View(new VerifyResetCodeViewModel { Email = email });
        }

       
        // POST: /Account/VerifyCode
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyResetCodeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var passenger = await _db.Passengers
                .FirstOrDefaultAsync(p => p.Email == model.Email);

            if (passenger == null)
            {
                ModelState.AddModelError("", "Invalid request.");
                return View(model);
            }

            var resetCode = await _db.PasswordResetCodes
                .Where(c => c.PassengerID == passenger.PassengerID
                         && c.Code == model.Code
                         && !c.IsUsed
                         && c.ExpiresAt > DateTime.Now)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (resetCode == null)
            {
                ModelState.AddModelError("Code", "Invalid or expired code. Please request a new one.");
                return View(model);
            }

            // Reset password
            passenger.PasswordHash = _pwd.HashPassword(model.NewPassword);
            resetCode.IsUsed = true;
            await _db.SaveChangesAsync();

            TempData["Message"] = "✅ Password reset successfully. Please login with your new password.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        [Authorize(Roles = "Passenger")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Passenger")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var passengerId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var passenger = await _db.Passengers.FindAsync(passengerId);
            if (passenger == null) return NotFound();

            // Verify current password
            if (!_pwd.VerifyPassword(model.CurrentPassword, passenger.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword",
                    "Current password is incorrect.");
                return View(model);
            }

            // Hash and save new password
            passenger.PasswordHash = _pwd.HashPassword(model.NewPassword);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Password changed successfully. " +
                                  "Please use the new password next time.";
            return RedirectToAction("Profile", "Passenger");
        }
    }
}