using System.Threading;
using System.Threading.Tasks;

namespace PowerPositionService.Core.Interfaces;

public interface IPowerPositionExtractor
{
    Task<bool> ExecuteExtractAsync(CancellationToken cancellationToken = default);
}
