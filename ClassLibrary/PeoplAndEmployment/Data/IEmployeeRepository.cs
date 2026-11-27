using ClassLibrary.PeoplAndEmployment.Models;

namespace ClassLibrary.PeoplAndEmployment.Data;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<EmployeeListItem>> GetAsync(DateTime asOfDate, string? search = null, int offset = 0, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<EmployeeOverview?> GetOverviewAsync(long employmentId, CancellationToken cancellationToken = default);
}
