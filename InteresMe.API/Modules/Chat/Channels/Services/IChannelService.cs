using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Chat.Channels.DTOs;

namespace InteresMe.API.Modules.Chat.Channels.Services;

public interface IChannelService
{
    Task<List<ChannelDto>> GetChannelsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ChannelDto>> CreateChannelAsync(
        Guid userId,
        CreateChannelRequest? request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ChannelDto>> GetChannelAsync(
        Guid userId,
        Guid channelId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ChannelDto>> AddMemberAsync(
        Guid userId,
        Guid channelId,
        Guid memberUserId,
        CancellationToken cancellationToken = default);
}
