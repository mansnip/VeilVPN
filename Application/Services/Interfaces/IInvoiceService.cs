using Domain.Entities;
using Domain.ViewModels.UserPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceViewModel> CreateInvoiceAsync(string userId, int traffic, int duration, SubscriptionPriceDetails priceDetails);
        Task<InvoiceViewModel> GetByIdAsync(string id);
        Task<List<InvoiceViewModel>> GetUserInvoicesAsync(string userId);

        // متد جدید
        Task<bool> UpdateInvoiceStatusAsync(string invoiceId, string newStatus);
    }

}
