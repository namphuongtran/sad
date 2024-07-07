using Microsoft.AspNetCore.Mvc;

namespace Quiz.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILogger<LeaderboardController> _logger;

        public LeaderboardController(ILogger<LeaderboardController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public void Get()
        {
            //Get leaderboard from database   
        }
    }
}
