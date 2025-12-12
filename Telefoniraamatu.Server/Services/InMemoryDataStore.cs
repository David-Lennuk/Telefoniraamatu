using System.Collections.Concurrent;
using Telefoniraamatu.Server.Models;

namespace Telefoniraamatu.Server.Services;

public class InMemoryDataStore : IDataStore
{
    public IDictionary<string, User> Users { get; } = new ConcurrentDictionary<string, User>();
    public IDictionary<string, Contact> Contacts { get; } = new ConcurrentDictionary<string, Contact>();
    public IDictionary<string, Group> Groups { get; } = new ConcurrentDictionary<string, Group>();
    public IList<SharedContact> SharedContacts { get; } = new List<SharedContact>();
    public IList<SharedGroup> SharedGroups { get; } = new List<SharedGroup>();
}











