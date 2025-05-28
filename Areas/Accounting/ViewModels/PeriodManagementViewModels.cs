using System;
using System.Collections.Generic;
using AspNetCoreMvcTemplate.Areas.Accounting.Services;

namespace AspNetCoreMvcTemplate.Areas.Accounting.ViewModels
{
    public class FiscalYearViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosed { get; set; }
    }

    public class FiscalPeriodViewModel
    {
        public Guid Id { get; set; }
        public Guid FiscalYearId { get; set; }
        public string FiscalYearName { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosed { get; set; }
    }

    public class FiscalPeriodsViewModel
    {
        public Guid FiscalYearId { get; set; }
        public string FiscalYearName { get; set; }
        public string FiscalYearCode { get; set; }
        public List<Models.FiscalPeriod> FiscalPeriods { get; set; }
    }

    public class YearEndClosingViewModel
    {
        public Guid FiscalYearId { get; set; }
        public string FiscalYearName { get; set; }
        public string FiscalYearCode { get; set; }
        public bool ConfirmClosing { get; set; }
        public YearEndClosingValidationResult ValidationResult { get; set; }
    }
}
