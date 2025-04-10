using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel
{
    public class SubscriptionDetails
    {
        public int Traffic { get; set; }
        public int Duration { get; set; }
        public decimal BasePrice { get; set; }
        public decimal PlanDiscountPercent { get; set; }
        public decimal PlanDiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
    }
}
