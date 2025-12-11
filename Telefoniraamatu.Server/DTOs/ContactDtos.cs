using Telefoniraamatu.Server.Models;
using System.Text.Json.Serialization;

namespace Telefoniraamatu.Server.DTOs;

public class ContactCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> PhoneNumbers { get; set; } = new();
    public List<string> Emails { get; set; } = new();
    public List<string> Addresses { get; set; } = new();
    public List<ContactField> CustomFields { get; set; } = new();
}

public class ContactResponse : Contact
{
}

public class ContactSimpleCreateRequest
{
    [JsonPropertyName("nimi")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("perekonnanimi")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("markmed")]
    public string Notes { get; set; } = string.Empty;

    [JsonPropertyName("telefon")]
    public string Phone { get; set; } = string.Empty;
}
