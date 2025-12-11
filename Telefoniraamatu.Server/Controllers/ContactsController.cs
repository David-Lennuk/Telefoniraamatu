using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telefoniraamatu.Server.DTOs;
using Telefoniraamatu.Server.Models;
using Telefoniraamatu.Server.Services;
using Swashbuckle.AspNetCore.Filters;

namespace Telefoniraamatu.Server.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly IDataStore _store;

    public ContactsController(IDataStore store)
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
    [SwaggerResponseExample(200, typeof(Telefoniraamatu.Server.Swagger.Examples.ContactResponseExample))]
    public ActionResult<IEnumerable<ContactResponse>> List([FromQuery] string? search = null)
    {
        var uid = ResolveUserId();
        var items = _store.Contacts.Values.Where(c => c.OwnerUserId == uid);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            items = items.Where(c =>
                c.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                c.PhoneNumbers.Any(p => p.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                c.Emails.Any(e => e.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                c.Addresses.Any(a => a.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                c.CustomFields.Any(f => f.TypeName.Contains(s, StringComparison.OrdinalIgnoreCase) || f.Value.Contains(s, StringComparison.OrdinalIgnoreCase))
            );
        }
        return Ok(items.Select(c => new ContactResponse
        {
            Id = c.Id,
            OwnerUserId = c.OwnerUserId,
            Name = c.Name,
            PhoneNumbers = c.PhoneNumbers,
            Emails = c.Emails,
            Addresses = c.Addresses,
            CustomFields = c.CustomFields
        }));
    }

    [HttpPost]
    [AllowAnonymous]
    [SwaggerResponseExample(201, typeof(Telefoniraamatu.Server.Swagger.Examples.ContactResponseExample))]
    public ActionResult<ContactResponse> Create([FromBody] ContactCreateRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Contact name is required");
        if (req.PhoneNumbers is null || !req.PhoneNumbers.Any(p => !string.IsNullOrWhiteSpace(p)))
            return BadRequest("Phone number is required");
        var uid = ResolveUserId();
        var contact = new Contact
        {
            OwnerUserId = uid,
            Name = req.Name,
            PhoneNumbers = req.PhoneNumbers,
            Emails = req.Emails,
            Addresses = req.Addresses,
            CustomFields = req.CustomFields
        };
        _store.Contacts[contact.Id] = contact;
        return CreatedAtAction(nameof(GetById), new { id = contact.Id }, contact);
    }

    [HttpPost("simple")]
    [AllowAnonymous]
    [SwaggerResponseExample(201, typeof(Telefoniraamatu.Server.Swagger.Examples.ContactResponseExample))]
    [Swashbuckle.AspNetCore.Filters.SwaggerRequestExample(typeof(ContactSimpleCreateRequest), typeof(Telefoniraamatu.Server.Swagger.Examples.ContactSimpleCreateRequestExample))]
    public ActionResult<ContactResponse> CreateSimple([FromBody] ContactSimpleCreateRequest req)
    {
        var first = (req.FirstName ?? string.Empty).Trim();
        var last = (req.LastName ?? string.Empty).Trim();
        var phone = (req.Phone ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
            return BadRequest("Name is required");
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest("Phone number is required");
        var name = string.Join(" ", new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var customFields = string.IsNullOrWhiteSpace(req.Notes)
            ? new List<ContactField>()
            : new List<ContactField> { new() { TypeName = "isikuandmed", Value = req.Notes! } };

        var uid = ResolveUserId();
        var contact = new Contact
        {
            OwnerUserId = uid,
            Name = name,
            PhoneNumbers = new List<string> { phone },
            Emails = new List<string>(),
            Addresses = new List<string>(),
            CustomFields = customFields
        };
        _store.Contacts[contact.Id] = contact;
        return CreatedAtAction(nameof(GetById), new { id = contact.Id }, contact);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [SwaggerResponseExample(200, typeof(Telefoniraamatu.Server.Swagger.Examples.ContactResponseExample))]
    public ActionResult<ContactResponse> GetById([FromRoute] string id)
    {
        var uid = ResolveUserId();
        if (!_store.Contacts.TryGetValue(id, out var contact) || contact.OwnerUserId != uid)
            return NotFound();
        return Ok(contact);
    }

    [HttpPut("{id}")]
    [AllowAnonymous]
    [SwaggerResponseExample(200, typeof(Telefoniraamatu.Server.Swagger.Examples.ContactResponseExample))]
    public ActionResult<ContactResponse> Update([FromRoute] string id, [FromBody] ContactCreateRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Contact name is required");
        if (req.PhoneNumbers is null || !req.PhoneNumbers.Any(p => !string.IsNullOrWhiteSpace(p)))
            return BadRequest("Phone number is required");
        var uid = ResolveUserId();
        if (!_store.Contacts.TryGetValue(id, out var contact) || contact.OwnerUserId != uid)
            return NotFound();
        contact.Name = req.Name;
        contact.PhoneNumbers = req.PhoneNumbers;
        contact.Emails = req.Emails;
        contact.Addresses = req.Addresses;
        contact.CustomFields = req.CustomFields;
        return Ok(contact);
    }

    [HttpDelete("{id}")]
    [AllowAnonymous]
    public ActionResult Delete([FromRoute] string id)
    {
        var uid = ResolveUserId();
        if (!_store.Contacts.TryGetValue(id, out var contact) || contact.OwnerUserId != uid)
            return NotFound();
        _store.Contacts.Remove(id);
        foreach (var g in _store.Groups.Values.Where(g => g.OwnerUserId == uid))
            g.ContactIds.Remove(id);
        return NoContent();
    }
}
