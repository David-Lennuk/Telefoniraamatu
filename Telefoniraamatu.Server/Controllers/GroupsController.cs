using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telefoniraamatu.Server.DTOs;
using Telefoniraamatu.Server.Models;
using Telefoniraamatu.Server.Services;
using Swashbuckle.AspNetCore.Filters;

namespace Telefoniraamatu.Server.Controllers;

[ApiController]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    private readonly IDataStore _store;

    public GroupsController(IDataStore store)
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

    [HttpGet]
    [AllowAnonymous]
    [SwaggerResponseExample(200, typeof(Telefoniraamatu.Server.Swagger.Examples.GroupExample))]
    public ActionResult<IEnumerable<Group>> List([FromQuery] string? search = null)
    {
        var uid = ResolveUserId();
        var items = _store.Groups.Values.Where(g => g.OwnerUserId == uid);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            items = items.Where(g =>
                g.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                g.ContactIds.Any(cid =>
                    _store.Contacts.TryGetValue(cid, out var c) && (
                        c.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumbers.Any(p => p.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                        c.Emails.Any(e => e.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                        c.Addresses.Any(a => a.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                        c.CustomFields.Any(f => f.TypeName.Contains(s, StringComparison.OrdinalIgnoreCase) || f.Value.Contains(s, StringComparison.OrdinalIgnoreCase))
                    )
                )
            );
        }
        return Ok(items);
    }

    [HttpPost]
    [AllowAnonymous]
    [SwaggerResponseExample(201, typeof(Telefoniraamatu.Server.Swagger.Examples.GroupExample))]
    public ActionResult<Group> Create([FromBody] GroupCreateRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Group name is required");
        var uid = ResolveUserId();
        var group = new Group { OwnerUserId = uid, Name = req.Name };
        _store.Groups[group.Id] = group;
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [SwaggerResponseExample(200, typeof(Telefoniraamatu.Server.Swagger.Examples.GroupExample))]
    public ActionResult<Group> GetById([FromRoute] string id)
    {
        var uid = ResolveUserId();
        if (!_store.Groups.TryGetValue(id, out var group) || group.OwnerUserId != uid)
            return NotFound();
        return Ok(group);
    }

    [HttpDelete("{id}")]
    [AllowAnonymous]
    public ActionResult Delete([FromRoute] string id)
    {
        var uid = ResolveUserId();
        if (!_store.Groups.TryGetValue(id, out var group) || group.OwnerUserId != uid)
            return NotFound();
        _store.Groups.Remove(id);
        return NoContent();
    }

    [HttpPost("{id}/contacts")]
    [AllowAnonymous]
    [SwaggerResponseExample(200, typeof(Telefoniraamatu.Server.Swagger.Examples.GroupExample))]
    public ActionResult<Group> AddContact([FromRoute] string id, [FromBody] AddContactToGroupRequest req)
    {
        var uid = ResolveUserId();
        if (!_store.Groups.TryGetValue(id, out var group) || group.OwnerUserId != uid)
            return NotFound();
        if (!_store.Contacts.TryGetValue(req.ContactId, out var contact) || contact.OwnerUserId != uid)
            return NotFound("Contact");
        group.ContactIds.Add(req.ContactId);
        return Ok(group);
    }

    [HttpDelete("{id}/contacts/{contactId}")]
    [AllowAnonymous]
    [SwaggerResponseExample(200, typeof(Telefoniraamatu.Server.Swagger.Examples.GroupExample))]
    public ActionResult<Group> RemoveContact([FromRoute] string id, [FromRoute] string contactId)
    {
        var uid = ResolveUserId();
        if (!_store.Groups.TryGetValue(id, out var group) || group.OwnerUserId != uid)
            return NotFound();
        group.ContactIds.Remove(contactId);
        return Ok(group);
    }
}
