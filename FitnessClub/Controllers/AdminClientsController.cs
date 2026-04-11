using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FitnessClub.Core.Requests;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/admin/clients")]
    [Authorize(Roles = "Admin")]
    public class AdminClientsController : ControllerBase
    {
        private readonly ClientService _clientService;
        private readonly ILogger<AdminClientsController> _logger;

        public AdminClientsController(ClientService clientService,ILogger<AdminClientsController> logger)
        {
            _clientService = clientService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
        {
            try
            {
                var client = await _clientService.CreateClientAsync(request);

                return Ok(new
                {
                    client.Id,
                    client.FirstName,
                    client.LastName,
                    client.Email,
                    client.Phone,
                    HasMembership = client.Membership != null,
                    Message = "Клиент успешно создан"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании клиента {Email}", request.Email);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] UpdateClientRequest request)
        {
            try
            {
                var client = await _clientService.UpdateClientAsync(id, request);

                return Ok(new
                {
                    client.Id,
                    client.FirstName,
                    client.LastName,
                    client.Email,
                    client.Phone,
                    Message = "Данные клиента обновлены"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении клиента {ClientId}", id);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchClients([FromQuery] string search = "")
        {
            try
            {
                var clients = await _clientService.SearchClientsAsync(search);
                var localToday = DateTime.Now.Date;
                var localTomorrow = localToday.AddDays(1);

                return Ok(clients.Select(c => new
                {
                    c.Id,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.Phone,
                    c.CreatedAt,
                    HasVisitedToday = c.Attendances.Any(a => a.CheckInTime >= localToday && a.CheckInTime < localTomorrow),
                    Membership = c.Membership == null ? null : new
                    {
                        c.Membership.Type,
                        c.Membership.StartDate,
                        c.Membership.EndDate,
                        c.Membership.IsActive,
                        c.Membership.RemainingVisits,
                        IsValid = c.Membership.IsValid()
                    }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске клиентов");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClientDetails(int id)
        {
            try
            {
                var client = await _clientService.GetClientDetailsAsync(id);

                if (client == null)
                {
                    _logger.LogWarning("Клиент с ID {ClientId} не найден", id);
                    return NotFound(new
                    {
                        Message = "Клиент не найден",
                        ClientId = id,
                        Status = "error"
                    });
                }

                return Ok(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении клиента {ClientId}", id);
                return BadRequest(ex.Message);
            }
        }
    }
}
