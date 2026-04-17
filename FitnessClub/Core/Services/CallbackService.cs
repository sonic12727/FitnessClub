using FitnessClub.Core.Entities;
using FitnessClub.Core.Requests;
using FitnessClub.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessClub.Core.Services
{
    public class CallbackService
    {
        private readonly FitnessClubDbContext _context;
        private readonly ILogger<CallbackService> _logger;

        public CallbackService(FitnessClubDbContext context, ILogger<CallbackService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Callback> CreateAsync(CreateCallbackRequest request)
        {
            var callback = new Callback
            {
                Name = request.Name.Trim(),
                Phone = request.Phone.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };

            await _context.Callbacks.AddAsync(callback);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Создана заявка на перезвон {CallbackId} для {Phone}", callback.Id, callback.Phone);

            return callback;
        }

        public async Task<List<Callback>> GetRecentPendingAsync(int count = 10)
        {
            return await _context.Callbacks.Where(x => !x.IsProcessed).OrderByDescending(x => x.CreatedAt).Take(count).ToListAsync();
        }

        public async Task MarkProcessedAsync(int id)
        {
            var callback = await _context.Callbacks.FirstOrDefaultAsync(x => x.Id == id);

            if (callback == null)
                throw new Exception("Заявка не найдена");

            callback.IsProcessed = true;
            await _context.SaveChangesAsync();
        }
    }
}
