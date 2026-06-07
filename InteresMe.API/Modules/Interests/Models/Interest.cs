namespace InteresMe.API.Modules.Interests.Models;

public sealed class Interest
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<UserInterest> UserInterests { get; set; } = [];
}
