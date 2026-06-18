namespace InteresMe.API.Modules.History.DTOs;

public sealed class HistoryResponse
{
    public IReadOnlyList<HistoryEventResponse> Events { get; set; } = [];
}
