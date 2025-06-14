using System.Collections.Generic;

namespace AspNetCoreMvcTemplate.DTOs
{
    public class CashFlowDTO
    {
        public OperatingActivitiesSection OperatingActivities { get; set; }
        public InvestingActivitiesSection InvestingActivities { get; set; }
        public FinancingActivitiesSection FinancingActivities { get; set; }
        public decimal NetCashFromOperating { get; set; }
        public decimal NetCashFromInvesting { get; set; }
        public decimal NetCashFromFinancing { get; set; }
        public decimal NetIncreaseInCash { get; set; }
        public decimal CashAtBeginning { get; set; }
        public decimal CashAtEnd { get; set; }
    }

    public class OperatingActivitiesSection
    {
        public decimal NetIncome { get; set; }
        public List<CashFlowItem> Adjustments { get; set; } = new List<CashFlowItem>();
        public List<CashFlowItem> ChangesInWorkingCapital { get; set; } = new List<CashFlowItem>();
        public decimal TotalAdjustments { get; set; }
        public decimal TotalChangesInWorkingCapital { get; set; }
    }

    public class InvestingActivitiesSection
    {
        public List<CashFlowItem> Items { get; set; } = new List<CashFlowItem>();
        public decimal TotalInvesting { get; set; }
    }

    public class FinancingActivitiesSection
    {
        public List<CashFlowItem> Items { get; set; } = new List<CashFlowItem>();
        public decimal TotalFinancing { get; set; }
    }

    public class CashFlowItem
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}
