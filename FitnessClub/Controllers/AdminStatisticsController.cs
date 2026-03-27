using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/admin/statistics")]
    [Authorize(Roles = "Admin")]
    public class AdminStatisticsController : ControllerBase
    {
        private readonly StatisticsService _statisticsService;
        private readonly ILogger<AdminStatisticsController> _logger;

        public AdminStatisticsController(StatisticsService statisticsService,ILogger<AdminStatisticsController> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var statistics = await _statisticsService.GetStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении общей статистики");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailedStatistics([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            try
            {
                if (from > to)
                {
                    return BadRequest(new { error = "Дата 'от' не может быть позже даты 'до'" });
                }

                var result = await _statisticsService.GetDetailedStatisticsAsync(from, to);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении детальной статистики");
                return BadRequest(new { error = "Не удалось загрузить статистику" });
            }
        }
    }
}
