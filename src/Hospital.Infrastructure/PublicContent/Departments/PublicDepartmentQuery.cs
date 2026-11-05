using Hospital.Application.Features.Departments;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.PublicContent.Departments;

public sealed class PublicDepartmentQuery : IPublicDepartmentQuery
{
    private readonly HospitalDbContext database;

    public PublicDepartmentQuery(HospitalDbContext database)
    {
        this.database = database;
    }

    public async Task<IReadOnlyList<DepartmentCard>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        return await database.Departments
            .AsNoTracking()
            .Where(department => department.PublicationState == DepartmentPublicationState.Published)
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentCard(
                department.Id,
                department.Name,
                department.Slug,
                department.ImagePath,
                department.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDetails?> FindPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await database.Departments
            .AsNoTracking()
            .Where(department =>
                department.Slug == normalizedSlug &&
                department.PublicationState == DepartmentPublicationState.Published)
            .Select(department => new DepartmentDetails(
                department.Id,
                department.Name,
                department.Slug,
                department.ImagePath,
                department.Description,
                department.Details,
                department.Doctors
                    .Where(doctor => doctor.PublicationState == DepartmentPublicationState.Published)
                    .OrderBy(doctor => doctor.Name)
                    .Select(doctor => new DepartmentDoctor(
                        doctor.Id,
                        doctor.Name,
                        doctor.Slug,
                        doctor.PhotoPath,
                        doctor.Qualifications,
                        doctor.Specialization))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
