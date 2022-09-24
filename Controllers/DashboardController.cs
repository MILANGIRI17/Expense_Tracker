using Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            //Doughnut Chart - Expense By Category
            ViewBag.DoughnutChartData = SelectedTransactions
            .Where(i => i.Category.Type == "Expense")
            .GroupBy(j => j.Category.CategoryId)
            .Select(k => new
            {
                categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                amount = k.Sum(x => x.Amount),
                formattedAmount = k.Sum(x => x.Amount).ToString("C0"),
            })
            .OrderByDescending(x => x.amount)
            .ToList();

            //Spline Chart - Income vs Expense
            //Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                                                .Where(x => x.Category.Type == "Income")
                                                .GroupBy(x => x.Date)
                                                .Select(x => new SplineChartData()
                                                {
                                                    day = x.First().Date.ToString("dd-MMM"),
                                                    income = x.Sum(x => x.Amount)
                                                }).ToList();
            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                                                .Where(x => x.Category.Type == "Expense")
                                                .GroupBy(x => x.Date)
                                                .Select(x => new SplineChartData()
                                                {
                                                    day = x.First().Date.ToString("dd-MMM"),
                                                    expense = x.Sum(x => x.Amount)
                                                }).ToList();
            //Combine Income & Expense
            string[] Last7Days = Enumerable.Range(0, 7)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            //Recent Transactions
            ViewBag.RecentTransactions=await _context.Transactions
                    .Include(i=>i.Category)
                    .OrderByDescending(x=>x.Date)
                    .Take(5)
                    .ToListAsync();

            return View();
        }

        public class SplineChartData
        {
            public string day;
            public int income;
            public int expense;
        }
    }
}
