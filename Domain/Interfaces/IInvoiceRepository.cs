using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice> GetByIdAsync(string id);
        Task<Invoice> GetByInvoiceNumberAsync(string invoiceNumber);
        Task<List<Invoice>> GetUserInvoicesAsync(string userId);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task<bool> UpdateAsync(Invoice invoice);
        Task<List<Invoice>> GetAllInvoicesWithUserAsync();
    }
}
