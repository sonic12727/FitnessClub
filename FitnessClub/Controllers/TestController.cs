namespace FitnessClub.Controllers
{
    // Controllers/TestController.cs
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                message = "Фитнес-клуб API работает!",
                time = DateTime.Now.ToString("HH:mm:ss"),
                date = DateTime.Now.ToString("dd.MM.yyyy")
            });
        }
    }
}
