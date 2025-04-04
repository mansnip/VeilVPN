using DataLayer.Context;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly VeilVpnDbContext _context; // نام DbContext شما

        // تزریق وابستگی DbContext
        public ExpenseRepository(VeilVpnDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // دریافت تمام هزینه‌ها (می‌توانید مرتب‌سازی پیش‌فرض را اضافه کنید)
        public async Task<IEnumerable<Expense>> GetAllAsync()
        {
            return await _context.Expenses
                                 .OrderByDescending(e => e.ExpenseDate) // مثال: مرتب‌سازی بر اساس تاریخ هزینه
                                 .AsNoTracking() // برای بهبود عملکرد در حالت فقط خواندنی
                                 .ToListAsync();
        }

        // دریافت یک هزینه بر اساس شناسه
        public async Task<Expense?> GetByIdAsync(string id)
        {
            return await _context.Expenses.FindAsync(id);
            // یا اگر نیاز به Include دارید:
            // return await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id);
        }

        // افزودن هزینه جدید
        public async Task AddAsync(Expense expense)
        {
            if (expense == null)
                throw new ArgumentNullException(nameof(expense));

            expense.CreatedDate = DateTime.UtcNow; // تنظیم تاریخ ایجاد
            await _context.Expenses.AddAsync(expense);
            // SaveChangesAsync در Service یا UnitOfWork فراخوانی می‌شود
        }

        // بروزرسانی هزینه موجود
        public Task UpdateAsync(Expense expense)
        {
            if (expense == null)
                throw new ArgumentNullException(nameof(expense));

            expense.ModifiedDate = DateTime.UtcNow; // تنظیم تاریخ ویرایش

            // EF Core به طور خودکار وضعیت موجودیت‌های Track شده را مدیریت می‌کند
            // اگر موجودیت Track نشده بود، باید آن را Attach کنید:
            // _context.Entry(expense).State = EntityState.Modified;
            _context.Expenses.Update(expense); // راه ساده‌تر برای علامت‌گذاری به عنوان ویرایش شده

            // SaveChangesAsync در Service یا UnitOfWork فراخوانی می‌شود
            return Task.CompletedTask; // این متد به طور ذاتی async نیست، اما برای سازگاری با اینترفیس async است
        }

        // حذف هزینه بر اساس شناسه
        public async Task DeleteAsync(string id)
        {
            var expense = await GetByIdAsync(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                // SaveChangesAsync در Service یا UnitOfWork فراخوانی می‌شود
            }
            // اگر هزینه پیدا نشد، می‌توان یک Exception پرتاب کرد یا به سادگی هیچ کاری نکرد
        }

        // ذخیره تغییرات (معمولاً در یک UnitOfWork یا در خود سرویس انجام می‌شود)
        // اگر UnitOfWork ندارید، می‌توانید این متد را در ریپازیتوری قرار دهید
        // و در سرویس‌ها پس از Add/Update/Delete فراخوانی کنید.
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
