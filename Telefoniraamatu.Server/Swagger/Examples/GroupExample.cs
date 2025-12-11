using Swashbuckle.AspNetCore.Filters;
using Telefoniraamatu.Server.Models;

namespace Telefoniraamatu.Server.Swagger.Examples;

public class GroupExample : IExamplesProvider<Group>
{
    public Group GetExamples()
    {
        return new Group
        {
            Id = "g1",
            OwnerUserId = "u1",
            Name = "Perekond",
            ContactIds = new HashSet<string> { "c1", "c2" }
        };
    }
}

