using System.Collections.Generic;

namespace AspNetCoreMvcTemplate.DTOs
{
    public class BalanceSheetDTO
    {
        public AssetsSection Assets { get; set; }
        public LiabilitiesSection Liabilities { get; set; }
        public EquitySection Equity { get; set; }
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public decimal TotalLiabilitiesAndEquity { get; set; }
    }

    public class AssetsSection
    {
        public List<AccountBalance> FixedAssets { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> CurrentAssets { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> OtherAssets { get; set; } = new List<AccountBalance>();
        public decimal TotalFixedAssets { get; set; }
        public decimal TotalCurrentAssets { get; set; }
        public decimal TotalOtherAssets { get; set; }
    }

    public class LiabilitiesSection
    {
        public List<AccountBalance> LongTermLiabilities { get; set; } = new List<AccountBalance>();
        public List<AccountBalance> CurrentLiabilities { get; set; } = new List<AccountBalance>();
        public decimal TotalLongTermLiabilities { get; set; }
        public decimal TotalCurrentLiabilities { get; set; }
    }

    public class EquitySection
    {
        public List<AccountBalance> EquityAccounts { get; set; } = new List<AccountBalance>();
        public decimal TotalEquity { get; set; }
    }

    public class AccountBalance
    {
        public string Code { get; set; }
        public string NameEn { get; set; }
        public decimal Balance { get; set; }
    }
}
