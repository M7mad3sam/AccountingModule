using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Services;
using AspNetCoreMvcTemplate.Models;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            // Create roles if they don't exist
            string[] roleNames = { "Admin", "Accountant", "Auditor", "Manager" };
            foreach (var roleName in roleNames)
            {
                if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
                {
                    roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
                }
            }

            // Create initial admin user if none exists
            if (!userManager.Users.Any())
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@application.com",
                    Email = "admin@application.com",
                    EmailConfirmed = true,
                    FullName = "Administrator",
                    DateOfBirth = DateTime.Today
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRolesAsync(adminUser, new[] { "Admin", "Manager" });
                }
            }
            else
            {
                // Ensure at least one admin exists
                var existingAdmins = await userManager.GetUsersInRoleAsync("Admin");
                if (!existingAdmins.Any())
                {
                    var firstUser = await userManager.Users.FirstOrDefaultAsync();
                    if (firstUser != null)
                    {
                        await userManager.AddToRoleAsync(firstUser, "Admin");
                    }
                }
            }
        }

        public static async Task EnsureDemoSeedAsync(IServiceProvider sp)
        {
            await using var ctx = sp.GetRequiredService<ApplicationDbContext>();
            
            if (await ctx.Accounts.AnyAsync()) return;              // already seeded

            var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var systemUser = await userMgr.FindByNameAsync("admin@application.com");
            if (systemUser is null)
            {
                systemUser = new ApplicationUser {
                    UserName = "seed@application.com",
                    Email = "seed@application.com",
                    EmailConfirmed = true,
                    FullName = "Seed",
                    DateOfBirth = DateTime.Today
                };
                await userMgr.CreateAsync(systemUser, "Seed123!");
            }

            var fp = ctx.FiscalPeriods.FirstOrDefault();

            #region 1. Accounts (11 rows)

            var faLand = NewAccount("1000", "Land", AccountType.Asset);
            var faEquip = NewAccount("1100", "Equipment", AccountType.Asset);
            var faAccDepEquip = NewAccount("1190", "Accum. Depreciation – Equip", AccountType.Asset);

            var caCash = NewAccount("2000", "Cash on Hand", AccountType.Asset);
            var caBank = NewAccount("2005", "Bank Current Acct", AccountType.Asset);
            var caAR = NewAccount("2100", "Accounts Receivable", AccountType.Asset);
            var caInventory = NewAccount("2200", "Inventory", AccountType.Asset);

            var ltLoan = NewAccount("4000", "Bank Loan (> 1 Year)", AccountType.Liability);
            var clAP = NewAccount("5000", "Accounts Payable", AccountType.Liability);

            var eqCapital = NewAccount("6000", "Owner Capital", AccountType.Equity);

            var revSales = NewAccount("7000", "Sales Revenue", AccountType.Revenue);
            var cogs = NewAccount("7100", "Cost of Goods Sold", AccountType.Expense);
            var expRent = NewAccount("7200", "Rent Expense", AccountType.Expense);
            var expDep = NewAccount("8800", "Depreciation Expense", AccountType.Expense); // non-cash for C/F


            ctx.AddRange(faLand, faEquip, faAccDepEquip,
                         caCash, caBank, caAR, caInventory,
                         ltLoan, clAP, eqCapital,
                         revSales, cogs, expRent, expDep);
            #endregion

            #region 2. Cost-centers (2 rows)

            var ccHQ = NewCC("CC-HQ", "Head Office");
            var ccBranch = NewCC("CC-BR", "Branch #1", ccHQ);   // child of HQ

            ctx.AddRange(ccHQ, ccBranch);
            #endregion

            #region 3. Account ↔ Cost-center links (simple: HQ = assets, Branch = revenue)

            Link(faEquip, ccHQ);
            Link(caCash, ccHQ);
            Link(caAR, ccBranch);
            Link(revSales, ccBranch);
            Link(expRent, ccBranch);
            #endregion

            #region 4. Journal Entries (6 JE / 12 lines)

            var je1 = NewJE("Capital Injection", new[]
            {
            Dr(caBank,  100_000m, ccHQ),
            Cr(eqCapital,100_000m, ccHQ)
        }, systemUser.Id, fp.Id);
            UpdateJournalEntryTotals(je1);

            var je2 = NewJE("Buy Equipment Cash", new[]
            {
            Dr(faEquip,  -30_000m, ccHQ),
            Cr(caBank,    30_000m, ccHQ)
        }, systemUser.Id, fp.Id);
            UpdateJournalEntryTotals(je2);

            var je3 = NewJE("Cash Sale", new[]
            {
            Dr(caCash,   18_000m,  ccBranch),
            Cr(revSales, 18_000m,  ccBranch)
        }, systemUser.Id, fp.Id);
            UpdateJournalEntryTotals(je3);

            var je4 = NewJE("Record COGS", new[]
            {
            Dr(cogs,       9_000m, ccBranch),
            Cr(caInventory,9_000m, ccBranch)
        }, systemUser.Id, fp.Id);
            UpdateJournalEntryTotals(je4);

            var je5 = NewJE("Pay Rent", new[]
            {
            Dr(expRent,  2_000m,   ccBranch),
            Cr(caCash,   2_000m,   ccBranch)
        }, systemUser.Id, fp.Id);
            UpdateJournalEntryTotals(je5);

            var je6 = NewJE("Monthly Depreciation", new[]
            {
            Dr(expDep,        500m,  ccHQ),
            Cr(faAccDepEquip, 500m,  ccHQ)
        }, systemUser.Id, fp.Id);
            UpdateJournalEntryTotals(je6);

            ctx.AddRange(je1, je2, je3, je4, je5, je6);
            #endregion

            await ctx.SaveChangesAsync();
        }

        // ----------  helpers ----------
        private static Account NewAccount(string code, string name, AccountType t) =>
            new() { Code = code, NameEn = name, NameAr = name, Type = t, IsActive = true };

        private static CostCenter NewCC(string code, string name, CostCenter? parent = null) =>
            new() { Code = code, NameEn = name, NameAr = name, IsActive = true, Parent = parent };

        private static void Link(Account a, CostCenter cc) =>
            a.AccountCostCenters = (a.AccountCostCenters ?? new List<AccountCostCenter>())
                             .Append(new AccountCostCenter { Account = a, CostCenter = cc }).ToList();

        static int voucherSeq = 1;
        private static JournalEntry NewJE(string desc, IEnumerable<JournalEntryLine> lines, string userId, Guid periodId) =>
            new()
            {
                Number = voucherSeq.ToString(),
                Reference = voucherSeq++.ToString(),
                FiscalPeriodId = periodId,
                Description = desc,
                TransactionDate = DateTime.Today,
                PostingDate = DateTime.Today,
                EntryDate = DateTime.Now,
                Status = JournalEntryStatus.Posted,
                Lines = lines.ToList(),
                CreatedById = userId,
                ModifiedById = userId
            };

        private static JournalEntryLine Dr(Account acc, decimal amount, CostCenter? cc = null) =>
            new() { Account = acc, Debit = Math.Abs(amount), Credit = 0m, CostCenter = cc };

        private static JournalEntryLine Cr(Account acc, decimal amount, CostCenter? cc = null) =>
            new() { Account = acc, Debit = 0m, Credit = Math.Abs(amount), CostCenter = cc };

        private static void UpdateJournalEntryTotals(JournalEntry jr)
        {
            jr.DebitTotal = jr.Lines.Sum(l => l.DebitAmount);
            jr.CreditTotal = jr.Lines.Sum(l => l.CreditAmount);
        }
    
    }
}
