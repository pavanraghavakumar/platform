namespace Hospital.Application.Features.Departments;

public interface IPublicDepartmentQuery
{
    Task<IReadOnlyList<DepartmentCard>> ListPublishedAsync(CancellationToken cancellationToken = default);

    Task<DepartmentDetails?> FindPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
