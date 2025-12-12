using Swashbuckle.AspNetCore.Filters;
using Telefoniraamatu.Server.DTOs;

namespace Telefoniraamatu.Server.Swagger.Examples;

public class AuthResponseExample : IExamplesProvider<AuthResponse>
{
    public AuthResponse GetExamples()
    {
        return new AuthResponse { Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." };
    }
}

