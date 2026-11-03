using Hms.Web.Data;
using Hms.Web.ViewModels.Audit;
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
        page = Math.Max(page, 1);
        recordType = string.IsNullOrWhiteSpace(recordType) ? null : recordType.Trim();

        var query = _db.AuditEntries.AsNoTracking().AsQueryable();
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

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Min(page, totalPages);

        var entries = await query
            .OrderByDescending(a => a.ChangedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(new AuditListViewModel
        {
            Entries = entries,
            RecordType = recordType,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }
}
