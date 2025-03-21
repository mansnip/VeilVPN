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
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly VeilVpnDbContext _context;

        public InvoiceRepository(VeilVpnDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice> GetByIdAsync(string id)
        {
            return await _context.Invoices
                .Include(i => i.User)
                .Include(i => i.Subscription)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .Include(i => i.User)
                .Include(i => i.Subscription)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<List<Invoice>> GetUserInvoicesAsync(string userId)
        {
            return await _context.Invoices
                .Include(i => i.Subscription)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.CreatedDate)
                .ToListAsync();
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            if (invoice == null)
                throw new ArgumentNullException(nameof(invoice));

            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task<bool> UpdateAsync(Invoice invoice)
        {
            try
            {
                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
    }
}
