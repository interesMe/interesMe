using System.Text.Json.Serialization;

namespace InteresMe.API.Modules.Posts.Feed.Contracts.Requests;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreatePostRequest
{
    public string? Body { get; set; }

    public Guid? InitiativeId { get; set; }
}
