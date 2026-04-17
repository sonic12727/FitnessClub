using FitnessClub.Core.Requests;
using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/public/callbacks")]
    public class PublicCallbacksController : ControllerBase
    {
        private readonly CallbackService _callbackService;
        private readonly ILogger<PublicCallbacksController> _logger;

        public PublicCallbacksController(CallbackService callbackService, ILogger<PublicCallbacksController> logger)
        {
            _callbackService = callbackService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCallbackRequest request)
        {
            try
            {
                var callback = await _callbackService.CreateAsync(request);

                return Ok(new
                {
                    callback.Id,
                    callback.Name,
                    callback.Phone,
                    callback.CreatedAt,
                    Message = "Заявка на перезвон успешно создана"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заявки на перезвон");
                return BadRequest(ex.Message);
            }
        }
    }
}
