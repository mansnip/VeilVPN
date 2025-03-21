using Domain.Entities.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Subscription
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public int Traffic { get; set; }
        public int Duration { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int RemainingTraffic { get; set; }
        public string InvoiceId { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Invoice Invoice { get; set; }
    }
}
