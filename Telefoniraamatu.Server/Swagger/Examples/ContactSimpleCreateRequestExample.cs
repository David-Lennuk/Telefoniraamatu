using Swashbuckle.AspNetCore.Filters;
using Telefoniraamatu.Server.DTOs;

namespace Telefoniraamatu.Server.Swagger.Examples;

public class ContactSimpleCreateRequestExample : IExamplesProvider<ContactSimpleCreateRequest>
{
    public ContactSimpleCreateRequest GetExamples()
    {
        return new ContactSimpleCreateRequest
        {
            FirstName = "Mari",
            LastName = "Maasikas",
            Notes = "Skype: mari.maasikas",
            Phone = "+3725123456"
        };
    }
}
