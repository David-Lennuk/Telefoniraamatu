namespace Telefoniraamatu.Server.Models;

public class SharedContact
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceOwnerUserId { get; set; } = string.Empty;
    public string ContactId { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
}

public class SharedGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceOwnerUserId { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
}

