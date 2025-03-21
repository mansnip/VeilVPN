using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel
{
    public class SubscriptionViewModel
    {
        public string Id { get; set; }
        public int Traffic { get; set; }
        public int Duration { get; set; }
        public int RemainingTraffic { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int DaysRemaining { get; set; }
        public int PercentTrafficUsed { get; set; }
        public string? UserId { get; set; }

    }
}
