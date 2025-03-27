using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Convertors
{
    public static class DateConvertor
    {
        public static long DayToUnixTimeMilliseconds(int days)
        {
            // تاریخ پایه (مثلاً امروز)
            DateTime baseDate = DateTime.UtcNow;

            // اضافه کردن تعداد روز به تاریخ پایه
            DateTimeOffset targetDate = baseDate.AddDays(days);

            // تبدیل به UnixTimeMilliseconds
            long unixTimeMilliseconds = targetDate.ToUnixTimeMilliseconds();

            return unixTimeMilliseconds;
        }
    }
}
