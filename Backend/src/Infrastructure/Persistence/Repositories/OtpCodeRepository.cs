using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class OtpCodeRepository : IOtpCodeRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public OtpCodeRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<OtpCode?> GetActiveOtpAsync(int userId, OtpPurpose purpose)
        {
            return await _dbContext.OtpCodes
                .Where(o => o.UserId == userId && o.Purpose == purpose && o.UsedAt == null && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public void Add(OtpCode otpCode)
        {
            _dbContext.OtpCodes.Add(otpCode);
        }

        public void Update(OtpCode otpCode)
        {
            _dbContext.OtpCodes.Update(otpCode);
        }

        public async Task InvalidatePreviousOtpsAsync(int userId, OtpPurpose purpose)
        {
            var activeOtps = await _dbContext.OtpCodes
                .Where(o => o.UserId == userId && o.Purpose == purpose && o.UsedAt == null)
                .ToListAsync();

            foreach (var otp in activeOtps)
            {
                otp.UsedAt = DateTime.UtcNow;
                _dbContext.OtpCodes.Update(otp);
            }
        }
    }
}
