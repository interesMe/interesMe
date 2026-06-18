using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.History.DTOs;
using InteresMe.API.Modules.History.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteresMe.API.Modules.History.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me/history")]
public sealed class UserHistoryController(
    IUserHistoryService userHistoryService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public Task<HistoryResponse> GetMyHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        userHistoryService.GetHistoryAsync(
            currentUser.UserId,
            page,
            pageSize,
            cancellationToken);
}
