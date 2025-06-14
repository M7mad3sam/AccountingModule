using System.Collections.Generic;

namespace AspNetCoreMvcTemplate.DTOs
{
    public class IncomeStatementDTO
    {
        public RevenueSection Revenue { get; set; }
        public ExpensesSection Expenses { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetIncome { get; set; }
    }

    public class RevenueSection
    {
        public List<AccountBalance> RevenueAccounts { get; set; } = new List<AccountBalance>();
        public decimal TotalRevenue { get; set; }
    }

    public class ExpensesSection
    {
        public List<AccountBalance> CostOfSales { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> OperatingExpenses { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> OtherIncomeExpense { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> TaxExpense { get; set; } = new List<AccountBalance>();
        public decimal TotalCostOfSales { get; set; }
        public decimal TotalOperatingExpenses { get; set; }
        public decimal TotalOtherIncomeExpense { get; set; }
        public decimal TotalTaxExpense { get; set; }
    }
}
