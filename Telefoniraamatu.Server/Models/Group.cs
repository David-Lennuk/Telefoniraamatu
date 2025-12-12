namespace Telefoniraamatu.Server.Models;

public class Group
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OwnerUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public HashSet<string> ContactIds { get; set; } = new();
}

