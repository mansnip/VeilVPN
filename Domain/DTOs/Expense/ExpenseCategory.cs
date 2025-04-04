using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Expense
{
    /// <summary>
    /// دسته‌بندی‌های مختلف برای هزینه‌ها
    /// </summary>
    public enum ExpenseCategory
    {
        [Display(Name = "ناشناخته")]
        Unknown = 0, // مقدار پیش‌فرض یا نامشخص

        [Display(Name = "خوراک و مواد غذایی")]
        FoodAndGroceries = 1,

        [Display(Name = "حمل و نقل")]
        Transportation = 2, // شامل بنزین، بلیط، تعمیرات خودرو و...

        [Display(Name = "مسکن")]
        Housing = 3, // شامل اجاره، قسط وام مسکن، شارژ ساختمان

        [Display(Name = "قبوض")]
        Utilities = 4, // آب، برق، گاز، تلفن، اینترنت

        [Display(Name = "پوشاک")]
        Clothing = 5,

        [Display(Name = "سلامت و درمان")]
        Healthcare = 6, // ویزیت پزشک، دارو، بیمه درمانی

        [Display(Name = "سرگرمی و تفریح")]
        Entertainment = 7, // سینما، رستوران، سفر، اشتراک سرویس‌ها

        [Display(Name = "آموزش")]
        Education = 8, // شهریه، کتاب، دوره‌های آموزشی

        [Display(Name = "هدایا و کمک‌های مالی")]
        GiftsAndDonations = 9,

        [Display(Name = "بیمه")]
        Insurance = 10, // بیمه‌های غیر درمانی مانند بیمه عمر، خودرو

        [Display(Name = "مراقبت شخصی")]
        PersonalCare = 11, // لوازم آرایشی، بهداشتی، پیرایشگاه

        [Display(Name = "بدهی")]
        Debt = 12, // بازپرداخت وام‌های غیر مسکن، کارت اعتباری

        [Display(Name = "سایر")]
        Other = 99 // برای موارد پیش‌بینی نشده
    }
}
