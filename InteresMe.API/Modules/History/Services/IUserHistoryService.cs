using InteresMe.API.Modules.History.DTOs;

namespace InteresMe.API.Modules.History.Services;

public interface IUserHistoryService
{
    Task AddEventAsync(
        AddUserHistoryEventRequest request,
        CancellationToken cancellationToken = default);

    Task<HistoryResponse> GetHistoryAsync(
        Guid userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
