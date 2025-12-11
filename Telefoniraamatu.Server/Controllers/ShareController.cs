using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telefoniraamatu.Server.Models;
using Telefoniraamatu.Server.Services;

namespace Telefoniraamatu.Server.Controllers;

[ApiController]
[Route("api/share")]
public class ShareController : ControllerBase
{
    private readonly IDataStore _store;

    public ShareController(IDataStore store)
    {
        _store = store;
    }

    private string ResolveUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId)) return userId;
        var anon = Request.Headers["X-Anonymous-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(anon) ? "anonymous" : $"anon:{anon}";
    }

    public record ShareRequest(string TargetUsername);

    [HttpPost("contact/{contactId}")]
    [Authorize]
    public ActionResult ShareContact([FromRoute] string contactId, [FromBody] ShareRequest req)
    {
        var uid = ResolveUserId();
        if (!_store.Contacts.TryGetValue(contactId, out var contact) || contact.OwnerUserId != uid)
            return NotFound();
        var target = _store.Users.Values.FirstOrDefault(u => u.Username.Equals(req.TargetUsername, StringComparison.OrdinalIgnoreCase));
        if (target is null) return NotFound("Target user");
        _store.SharedContacts.Add(new SharedContact
        {
            SourceOwnerUserId = uid,
            ContactId = contactId,
            TargetUserId = target.Id
        });
        return Ok();
    }

    [HttpPost("group/{groupId}")]
    [Authorize]
    public ActionResult ShareGroup([FromRoute] string groupId, [FromBody] ShareRequest req)
    {
        var uid = ResolveUserId();
        if (!_store.Groups.TryGetValue(groupId, out var group) || group.OwnerUserId != uid)
            return NotFound();
        var target = _store.Users.Values.FirstOrDefault(u => u.Username.Equals(req.TargetUsername, StringComparison.OrdinalIgnoreCase));
        if (target is null) return NotFound("Target user");
        _store.SharedGroups.Add(new SharedGroup
        {
            SourceOwnerUserId = uid,
            GroupId = groupId,
            TargetUserId = target.Id
        });
        return Ok();
    }

    public record SharedResponse(IEnumerable<Contact> Contacts, IEnumerable<Group> Groups);

    [HttpGet("with-me")]
    [Authorize]
    public ActionResult<SharedResponse> SharedWithMe()
    {
        var uid = ResolveUserId();
        var contacts = _store.SharedContacts.Where(s => s.TargetUserId == uid)
            .Select(s => _store.Contacts.TryGetValue(s.ContactId, out var c) ? c : null)
            .Where(c => c is not null)!
            .ToList();
        var groups = _store.SharedGroups.Where(s => s.TargetUserId == uid)
            .Select(s => _store.Groups.TryGetValue(s.GroupId, out var g) ? g : null)
            .Where(g => g is not null)!
            .ToList();
        return Ok(new SharedResponse(contacts!, groups!));
    }
}

