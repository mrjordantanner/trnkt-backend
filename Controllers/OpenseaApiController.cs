using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace trnkt_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OpenseaApiController : ControllerBase
    {
        private readonly ILogger<OpenseaApiController> _logger;

        public WeatherForecastController(ILogger<OpenseaApiController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("- Info log test -");
            return Ok("Hello from ASP.NET Core API");
        }


    }
}
