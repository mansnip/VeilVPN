using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel
{
    public class SubscriptionStatusViewModel
    {
        public bool HasActiveSubscription { get; set; }
        public int RemainingTraffic { get; set; }
        public int RemainingDays { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
