using Swashbuckle.AspNetCore.Filters;
using Telefoniraamatu.Server.DTOs;
using Telefoniraamatu.Server.Models;

namespace Telefoniraamatu.Server.Swagger.Examples;

public class ContactResponseExample : IExamplesProvider<ContactResponse>
{
    public ContactResponse GetExamples()
    {
        return new ContactResponse
        {
            Id = "c1",
            OwnerUserId = "u1",
            Name = "Mari Maasikas",
            PhoneNumbers = new List<string> { "+3725123456" },
            Emails = new List<string> { "mari@example.com" },
            Addresses = new List<string> { "Tartu, Eesti" },
            CustomFields = new List<ContactField> { new() { TypeName = "Skype", Value = "mari.maasikas" } }
        };
    }
}

