using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/admin/callbacks")]
    [Authorize(Roles = "Admin")]
    public class AdminCallbacksController : ControllerBase
    {
        private readonly CallbackService _callbackService;
        private readonly ILogger<AdminCallbacksController> _logger;

        public AdminCallbacksController(CallbackService callbackService, ILogger<AdminCallbacksController> logger)
        {
            _callbackService = callbackService;
            _logger = logger;
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent()
        {
            try
            {
                var callbacks = await _callbackService.GetRecentPendingAsync(10);

                return Ok(callbacks.Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Phone,
                    x.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заявок на перезвон");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/processed")]
        public async Task<IActionResult> MarkProcessed(int id)
        {
            try
            {
                await _callbackService.MarkProcessedAsync(id);
                return Ok(new { Message = "Заявка обработана" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке заявки {CallbackId}", id);
                return BadRequest(ex.Message);
            }
        }
    }
}
