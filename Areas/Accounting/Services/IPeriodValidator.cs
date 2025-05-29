using System;
using System.Threading.Tasks;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IPeriodValidator
    {
        Task<bool> IsPeriodClosedAsync(DateTime date);
    }
}
