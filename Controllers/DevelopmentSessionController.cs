using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using samane.Models;
using samane.Services.Interfaces;
namespace samane.Controllers
{
    [ApiController, Route("api/development-session")]
    public class DevelopmentSessionController : ControllerBase
    {
        private readonly IHostingEnvironment env;
        private readonly IConfiguration config;
        private readonly IDevelopmentSessionService service;
        public DevelopmentSessionController(IHostingEnvironment env, IConfiguration config,
                                            IDevelopmentSessionService service)
        {
            this.env = env;
            this.config = config;
            this.service = service;
        }
        private bool Enabled => env.IsDevelopment() && config.GetValue<bool>("DevelopmentAuth:Enabled");
        [AllowAnonymous, HttpGet("database")]
        public async Task<IActionResult> Database(CancellationToken ct)
        {
            if (!Enabled)
                return NotFound();
            await service.CheckDatabaseAsync(ct);
            return Ok(new { connected = true });
        }
        [AllowAnonymous, HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DevelopmentLoginInput input, CancellationToken ct)
        {
            if (!Enabled)
                return NotFound();
            if (Request.Headers["X-Requested-With"] != "PersonnelReview")
                return BadRequest();
            var principal = await service.AuthenticateAsync(input.Login, input.Password, ct);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = false,
                                               ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) });
            return Ok(new { signedIn = true });
        }
        [Authorize, HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (!Enabled)
                return NotFound();
            if (Request.Headers["X-Requested-With"] != "PersonnelReview")
                return BadRequest();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }
    }
}
