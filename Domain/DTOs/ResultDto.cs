using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs
{
    /// <summary>
    /// کلاس پایه برای بازگرداندن نتیجه عملیات بدون داده خاص.
    /// </summary>
    public class ResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        // سازنده پیش‌فرض برای سریال‌سازی و موارد دیگر
        public ResultDto() { }

        protected ResultDto(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        // ---------- متدهای کمکی استاتیک ----------

        /// <summary>
        /// ایجاد یک نتیجه موفقیت‌آمیز بدون داده.
        /// </summary>
        /// <param name="message">پیام موفقیت (اختیاری).</param>
        public static ResultDto Ok(string message = "عملیات با موفقیت انجام شد.")
        {
            return new ResultDto(true, message);
        }

        /// <summary>
        /// ایجاد یک نتیجه ناموفق.
        /// </summary>
        /// <param name="message">پیام خطا (الزامی).</param>
        public static ResultDto Fail(string message)
        {
            // اطمینان از اینکه پیام خطا خالی نیست
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "خطای نامشخصی رخ داده است.";
            }
            return new ResultDto(false, message);
        }

        /// <summary>
        /// ایجاد یک نتیجه ناموفق از روی یک Exception.
        /// </summary>
        /// <param name="exception">اکسپشن رخ داده.</param>
        /// <param name="customMessage">پیام خطای سفارشی (اختیاری).</param>
        public static ResultDto Fail(Exception exception, string customMessage = null)
        {
            var errorMessage = customMessage ?? "خطای سیستمی رخ داد.";
            // در محیط Production، جزئیات Exception نباید به کاربر نمایش داده شود، فقط لاگ شود.
            // errorMessage += $" (جزئیات فنی: {exception.Message})"; // این خط را فقط در حالت Debug فعال کنید
            return new ResultDto(false, errorMessage);
        }
    }

    /// <summary>
    /// کلاس جنریک برای بازگرداندن نتیجه عملیات به همراه داده.
    /// </summary>
    /// <typeparam name="TData">نوع داده‌ای که در صورت موفقیت بازگردانده می‌شود.</typeparam>
    public class ResultDto<TData> : ResultDto
    {
        public TData Data { get; set; }

        // سازنده پیش‌فرض
        public ResultDto() : base() { }

        // سازنده داخلی برای استفاده توسط متدهای استاتیک
        private ResultDto(bool isSuccess, string message, TData data) : base(isSuccess, message)
        {
            Data = data;
        }

        // ---------- متدهای کمکی استاتیک ----------

        /// <summary>
        /// ایجاد یک نتیجه موفقیت‌آمیز به همراه داده.
        /// </summary>
        /// <param name="data">داده‌ای که باید بازگردانده شود.</param>
        /// <param name="message">پیام موفقیت (اختیاری).</param>
        public static ResultDto<TData> Ok(TData data, string message = "عملیات با موفقیت انجام شد.")
        {
            return new ResultDto<TData>(true, message, data);
        }

        /// <summary>
        /// ایجاد یک نتیجه ناموفق (بدون داده).
        /// </summary>
        /// <param name="message">پیام خطا.</param>
        public new static ResultDto<TData> Fail(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "خطای نامشخصی رخ داده است.";
            }
            // چون ناموفق است، Data مقدار پیش‌فرض خود را خواهد داشت (مثلاً null برای reference type ها)
            return new ResultDto<TData>(false, message, default(TData));
        }

        /// <summary>
        /// ایجاد یک نتیجه ناموفق از روی Exception (بدون داده).
        /// </summary>
        /// <param name="exception">اکسپشن رخ داده.</param>
        /// <param name="customMessage">پیام خطای سفارشی (اختیاری).</param>
        public new static ResultDto<TData> Fail(Exception exception, string customMessage = null)
        {
            var errorMessage = customMessage ?? "خطای سیستمی رخ داد.";
            // Log exception here
            return new ResultDto<TData>(false, errorMessage, default(TData));
        }
    }
}
