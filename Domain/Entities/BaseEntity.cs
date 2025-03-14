using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class BaseEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreateDateTime { get; set; } = DateTime.Now;
        [Display(Name = "حساب حذف شده؟")]
        public bool IsDelete { get; set; } = false;
    }
}
