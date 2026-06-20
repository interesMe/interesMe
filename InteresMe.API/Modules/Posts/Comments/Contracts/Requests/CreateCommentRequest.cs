using System.Text.Json.Serialization;

namespace InteresMe.API.Modules.Posts.Comments.Contracts.Requests;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateCommentRequest
{
    public string? Body { get; set; }
}
