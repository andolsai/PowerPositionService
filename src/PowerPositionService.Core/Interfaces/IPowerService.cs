using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PowerPositionService.Core.Models;

namespace PowerPositionService.Core.Interfaces
{
    public interface IPowerService
    {
        Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date);
    }
}
