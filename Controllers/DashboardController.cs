using Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            //Last 7 Days
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            //Total Income
            int TotalIncome = SelectedTransactions.Where(x => x.Category.Type == "Income").Sum(x => x.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("c0");
            //Total Expense
            int TotalExpense = SelectedTransactions.Where(x => x.Category.Type == "Expense").Sum(x => x.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("c0");

            //Balance
            int Balance = TotalIncome - TotalExpense;
            ViewBag.Balance = Balance.ToString("c0");

            return View();
        }
    }
}
