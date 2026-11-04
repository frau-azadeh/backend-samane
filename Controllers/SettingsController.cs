using Microsoft.AspNetCore.Mvc;
using samane.Configuration;

namespace samane.Controllers
{
    [Route("api/settings")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly RuntimeSettings settings;
        public SettingsController(RuntimeSettings settings)
        {
            this.settings = settings;
        }
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Get() => Ok(new { apiBaseUrl = settings.ApiBaseUrl });
    }
}
