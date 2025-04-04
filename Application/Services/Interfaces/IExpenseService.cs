using Domain.DTOs.Expense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IExpenseService
    {
        Task<IEnumerable<ExpenseViewModel>> GetAllExpensesAsync();
        Task<ExpenseViewModel?> GetExpenseByIdAsync(string id);
        Task<(bool Success, string? ErrorMessage)> UpdateExpenseAsync(CreateEditExpenseViewModel viewModel);
        Task<(bool Success, string? ErrorMessage)> DeleteExpenseAsync(string id);
        Task<CreateEditExpenseViewModel?> GetExpenseForEditAsync(string id); // برای پر کردن فرم ویرایش
        Task<(bool Success, string? ErrorMessage, string? CreatedId)> CreateExpenseAsync(CreateEditExpenseViewModel viewModel);

    }

    public class Result { public bool Succeeded { get; set; } public List<string> Errors { get; set; } = new(); }
}
