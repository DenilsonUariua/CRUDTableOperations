using CRUDTableOperations.Core.Models;

namespace CRUDTableOperations.Core.Contracts.Services;

public interface ISampleDataService
{
    Task<IEnumerable<SampleOrder>> GetGridDataAsync();
}
