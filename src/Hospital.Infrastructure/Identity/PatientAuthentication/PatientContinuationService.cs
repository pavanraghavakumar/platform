using Hospital.Application.Features.PatientAuthentication;
using Microsoft.Extensions.Caching.Memory;

namespace Hospital.Infrastructure.Identity.PatientAuthentication;

public sealed class PatientContinuationService : IPatientContinuationService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(20);
    private readonly IMemoryCache cache;

    public PatientContinuationService(IMemoryCache cache)
    {
        this.cache = cache;
    }

    public string Create(string localDestination)
    {
        if (!IsLocalDestination(localDestination))
        {
            throw new ArgumentException("The continuation destination must be local.", nameof(localDestination));
        }

        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        cache.Set(CacheKey(token), localDestination, Lifetime);
        return token;
    }

    public string? Consume(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !cache.TryGetValue(CacheKey(token), out string? destination))
        {
            return null;
        }

        cache.Remove(CacheKey(token));
        return IsLocalDestination(destination) ? destination : null;
    }

    private static string CacheKey(string token) => $"patient-continuation:{token}";

    private static bool IsLocalDestination(string? destination) =>
        !string.IsNullOrWhiteSpace(destination)
        && destination[0] == '/'
        && (destination.Length == 1 || (destination[1] != '/' && destination[1] != '\\'));
}
