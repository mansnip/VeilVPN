using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extensions
{
    public static class NumberExtensions
    {
        public static string ToPrice(this decimal price)
        {
            return price.ToString("#,##0");
        }

        public static string ToPrice(this int price)
        {
            return price.ToString("#,##0");
        }
    }
}
