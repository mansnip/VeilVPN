using Domain.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IDiscountService
    {
        Task<ResultDto<DiscountCode>> ValidateDiscountCodeAsync(string code, string userId, string invoiceId);
        Task MarkCodeAsUsedAsync(DiscountCode discountCode); // برای افزایش CurrentUses
    }
}
