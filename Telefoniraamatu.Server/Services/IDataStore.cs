using Telefoniraamatu.Server.Models;

namespace Telefoniraamatu.Server.Services;

public interface IDataStore
{
    IDictionary<string, User> Users { get; }
    IDictionary<string, Contact> Contacts { get; }
    IDictionary<string, Group> Groups { get; }
    IList<SharedContact> SharedContacts { get; }
    IList<SharedGroup> SharedGroups { get; }
}

