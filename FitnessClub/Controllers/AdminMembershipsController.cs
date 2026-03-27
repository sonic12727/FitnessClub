using FitnessClub.Core.Enums;
using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FitnessClub.Core.Requests;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/admin/memberships")]
    [Authorize(Roles = "Admin")]
    public class AdminMembershipsController : ControllerBase
    {
        private readonly MembershipService _membershipService;
        private readonly ILogger<AdminMembershipsController> _logger;

        public AdminMembershipsController(MembershipService membershipService,ILogger<AdminMembershipsController> logger)
        {
            _membershipService = membershipService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> AddMembership([FromBody] AddMembershipRequest request)
        {
            try
            {
                if (!Enum.TryParse<MembershipType>(request.Type, out var membershipType))
                {
                    return BadRequest("Некорректный тип абонемента. Доступные: OneTime, Monthly, Quarterly, Yearly");
                }

                var membership = await _membershipService.AddMembershipAsync(
                    request.UserId,
                    membershipType,
                    request.DurationMonths,
                    request.Price);

                return Ok(new
                {
                    membership.Id,
                    membership.Type,
                    membership.StartDate,
                    membership.EndDate,
                    membership.Price,
                    membership.IsActive,
                    membership.RemainingVisits,
                    Message = "Абонемент успешно добавлен/обновлён"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выдаче абонемента пользователю {UserId}", request.UserId);
                return BadRequest(ex.Message);
            }
        }
    }
}
