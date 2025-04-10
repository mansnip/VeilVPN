using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IDiscountCodeRepository
    {
        Task<DiscountCode?> GetByCodeAsync(string code);
        Task<bool> UpdateAsync(DiscountCode discountCode);
        // سایر متدهای مورد نیاز (مثلا AddAsync, GetAllAsync برای ادمین و...)
    }
}
