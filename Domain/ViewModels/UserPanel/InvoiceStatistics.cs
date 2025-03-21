using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel
{
    public class InvoiceStatistics
    {
        public int TotalInvoices { get; set; }
        public int PendingInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public int CanceledInvoices { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
