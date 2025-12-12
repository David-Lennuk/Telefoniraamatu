namespace Telefoniraamatu.Server.DTOs;

public class GroupCreateRequest
{
    public string Name { get; set; } = string.Empty;
}

public class AddContactToGroupRequest
{
    public string ContactId { get; set; } = string.Empty;
}

