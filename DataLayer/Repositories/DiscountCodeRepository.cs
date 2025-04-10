using DataLayer.Context;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Repositories
{
    public class DiscountCodeRepository : IDiscountCodeRepository
    {
        private readonly VeilVpnDbContext _context;

        public DiscountCodeRepository(VeilVpnDbContext context)
        {
            _context = context;
        }

        public async Task<DiscountCode?> GetByCodeAsync(string code)
        {
            // کد را بدون حساسیت به بزرگی/کوچکی حروف جستجو کن
            return await _context.DiscountCodes
                                 .FirstOrDefaultAsync(dc => dc.Code.ToUpper() == code.ToUpper());
        }

        public async Task<bool> UpdateAsync(DiscountCode discountCode)
        {
            _context.DiscountCodes.Update(discountCode);
            return await _context.SaveChangesAsync() > 0;
        }
        // ... پیاده‌سازی سایر متدها ...
    }
}
