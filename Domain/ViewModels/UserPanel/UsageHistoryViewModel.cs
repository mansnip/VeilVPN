using System;

namespace Domain.ViewModels.UserPanel
{
    public class UsageHistoryViewModel
    {
        public DateTime Date { get; set; }
        public double UsageGB { get; set; } // مصرف بر حسب گیگابایت
    }
}