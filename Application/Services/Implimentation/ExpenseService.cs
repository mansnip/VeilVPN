using Application.Services.Interfaces;
using Domain.DTOs.Expense;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implimentation
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepository;
        // private readonly IMapper _mapper; // اگر از AutoMapper استفاده می‌کنید

        // تزریق وابستگی ریپازیتوری (و Mapper در صورت استفاده)
        public ExpenseService(IExpenseRepository expenseRepository /*, IMapper mapper*/)
        {
            _expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
            // _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // دریافت تمام هزینه‌ها به صورت ViewModel
        public async Task<IEnumerable<ExpenseViewModel>> GetAllExpensesAsync()
        {
            var expenses = await _expenseRepository.GetAllAsync();

            // تبدیل دستی Entity به ViewModel
            return expenses.Select(e => new ExpenseViewModel
            {
                Id = e.Id,
                Description = e.Description,
                Amount = e.Amount,
                ExpenseDate = e.ExpenseDate,
                Category = e.Category,
                Frequency = e.Frequency,
                Notes = e.Notes
                // FrequencyDisplayName و FormattedExpenseDate توسط پراپرتی‌های get در ViewModel محاسبه می‌شوند
            });

            // با AutoMapper:
            // return _mapper.Map<IEnumerable<ExpenseViewModel>>(expenses);
        }

        // دریافت یک هزینه بر اساس شناسه به صورت ViewModel
        public async Task<ExpenseViewModel?> GetExpenseByIdAsync(string id)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null)
                return null;

            // تبدیل دستی Entity به ViewModel
            return new ExpenseViewModel
            {
                Id = expense.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                Category = expense.Category,
                Frequency = expense.Frequency,
                Notes = expense.Notes
            };

            // با AutoMapper:
            // return _mapper.Map<ExpenseViewModel>(expense);
        }

        // دریافت یک هزینه برای ویرایش به صورت CreateEditExpenseViewModel
        public async Task<CreateEditExpenseViewModel?> GetExpenseForEditAsync(string id)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null)
                return null;

            // تبدیل دستی Entity به ViewModel
            return new CreateEditExpenseViewModel
            {
                Id = expense.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                Category = expense.Category,
                Frequency = expense.Frequency,
                Notes = expense.Notes
            };

            // با AutoMapper:
            // return _mapper.Map<CreateEditExpenseViewModel>(expense);
        }


        // ایجاد هزینه جدید از ViewModel
        public async Task<(bool Success, string? ErrorMessage, string? CreatedId)> CreateExpenseAsync(CreateEditExpenseViewModel viewModel)
        {
            try
            {
                // تبدیل دستی ViewModel به Entity
                var expense = new Expense
                {
                    // Id خودکار توسط دیتابیس تولید می‌شود
                    Description = viewModel.Description,
                    Amount = viewModel.Amount,
                    ExpenseDate = viewModel.ExpenseDate,
                    Category = viewModel.Category,
                    Frequency = viewModel.Frequency,
                    Notes = viewModel.Notes
                    // CreatedDate و ModifiedDate در ریپازیتوری تنظیم می‌شوند
                };

                // با AutoMapper:
                // var expense = _mapper.Map<Expense>(viewModel);

                await _expenseRepository.AddAsync(expense);
                await _expenseRepository.SaveChangesAsync(); // ذخیره تغییرات

                return (true, null, expense.Id); // بازگرداندن موفقیت و ID هزینه ایجاد شده
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                Console.WriteLine($"Error creating expense: {ex.Message}"); // لاگ ساده
                return (false, "خطایی در هنگام ثبت هزینه رخ داد.", null);
            }
        }

        // بروزرسانی هزینه موجود از ViewModel
        public async Task<(bool Success, string? ErrorMessage)> UpdateExpenseAsync(CreateEditExpenseViewModel viewModel)
        {
            try
            {
                var existingExpense = await _expenseRepository.GetByIdAsync(viewModel.Id);
                if (existingExpense == null)
                {
                    return (false, "هزینه مورد نظر یافت نشد.");
                }

                // بروزرسانی مقادیر موجودیت با مقادیر ViewModel
                existingExpense.Description = viewModel.Description;
                existingExpense.Amount = viewModel.Amount;
                existingExpense.ExpenseDate = viewModel.ExpenseDate;
                existingExpense.Category = viewModel.Category;
                existingExpense.Frequency = viewModel.Frequency;
                existingExpense.Notes = viewModel.Notes;
                // ModifiedDate در ریپازیتوری تنظیم می‌شود

                // با AutoMapper:
                // _mapper.Map(viewModel, existingExpense);

                await _expenseRepository.UpdateAsync(existingExpense);
                await _expenseRepository.SaveChangesAsync(); // ذخیره تغییرات

                return (true, null); // بازگرداندن موفقیت
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                Console.WriteLine($"Error updating expense (ID: {viewModel.Id}): {ex.Message}"); // لاگ ساده
                return (false, "خطایی در هنگام بروزرسانی هزینه رخ داد.");
            }
        }

        // حذف هزینه بر اساس شناسه
        public async Task<(bool Success, string? ErrorMessage)> DeleteExpenseAsync(string id)
        {
            try
            {
                var expense = await _expenseRepository.GetByIdAsync(id);
                if (expense == null)
                {
                    // شاید بهتر باشد که اگر وجود نداشت هم موفقیت برگردانیم چون نتیجه یکی است (هزینه دیگر وجود ندارد)
                    // return (false, "هزینه مورد نظر یافت نشد.");
                    return (true, null); // هزینه از قبل وجود نداشته، پس عملیات موفقیت‌آمیز تلقی می‌شود.
                }

                await _expenseRepository.DeleteAsync(id);
                await _expenseRepository.SaveChangesAsync(); // ذخیره تغییرات

                return (true, null); // بازگرداندن موفقیت
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                Console.WriteLine($"Error deleting expense (ID: {id}): {ex.Message}"); // لاگ ساده
                return (false, "خطایی در هنگام حذف هزینه رخ داد.");
            }
        }
    }
}
