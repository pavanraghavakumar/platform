using Hms.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AuditController : Controller
{
    private readonly HmsDbContext _db;

    public AuditController(HmsDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? recordType, DateTime? from, DateTime? to, int page = 1)
    {
        const int pageSize = 20;
        var query = _db.AuditEntries.AsQueryable();
        if (!string.IsNullOrWhiteSpace(recordType))
        {
            query = query.Where(a => a.RecordType == recordType);
        }

        if (from is not null)
        {
            query = query.Where(a => a.ChangedAt >= from.Value.Date);
        }

        if (to is not null)
        {
            query = query.Where(a => a.ChangedAt < to.Value.Date.AddDays(1));
        }

        ViewData["RecordType"] = recordType;
        ViewData["From"] = from;
        ViewData["To"] = to;
        ViewData["Page"] = page;
        return View(await query.OrderByDescending(a => a.ChangedAt).Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync());
    }
}
