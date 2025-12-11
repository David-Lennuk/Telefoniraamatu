namespace Telefoniraamatu.Server.Models;

public class Contact
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OwnerUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> PhoneNumbers { get; set; } = new();
    public List<string> Emails { get; set; } = new();
    public List<string> Addresses { get; set; } = new();
    public List<ContactField> CustomFields { get; set; } = new();
}

public class ContactField
{
    public string TypeName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

