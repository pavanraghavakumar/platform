using Hospital.Application.Contracts;
using Hospital.Application.Features.About;
using Hospital.Application.Features.HealthPackages;
using Hospital.Application.Features.PatientAuthentication;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Identity;
using Hospital.Infrastructure.Identity.PatientAuthentication;
using Hospital.Infrastructure.Messaging;
using Hospital.Infrastructure.Monitoring;
using Hospital.Infrastructure.PublicContent.About;
using Hospital.Infrastructure.PublicContent.HealthPackages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Web.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHospitalServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HospitalDatabase")
            ?? "Data Source=App_Data/hospital.db";

        services.AddDbContext<HospitalDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<
            Hospital.Application.Features.MedicalServices.IMedicalServicesQuery,
            Hospital.Application.Features.MedicalServices.MedicalServicesQuery>();
        services.AddScoped<
            Hospital.Application.Features.MedicalServices.IMedicalServicePublicStore,
            Hospital.Infrastructure.PublicContent.MedicalServices.SqliteMedicalServicePublicStore>();
        services.AddScoped<
            Hospital.Application.Features.Departments.IPublicDepartmentQuery,
            Hospital.Infrastructure.PublicContent.Departments.PublicDepartmentQuery>();
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<HospitalDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.SlidingExpiration = true;
            options.AccessDeniedPath = "/forbidden";
            options.LoginPath = "/patient/login";
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        services.AddMemoryCache();
        services.AddHealthChecks().AddDbContextCheck<HospitalDbContext>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPatientContinuationService, PatientContinuationService>();
        services.AddScoped<IPatientAuthenticationService, IdentityPatientAuthenticationService>();
        services.AddScoped<IEmailSender, DevelopmentEmailSender>();
        services.AddScoped<IOperationalMonitor, OperationalMonitor>();
        services.AddSingleton<IAboutContentQuery, StaticAboutContentQuery>();
        services.AddScoped<IHealthPackageContentSource, SqliteHealthPackageContentSource>();
        services.AddScoped<IPublicHealthPackagesQuery, PublicHealthPackagesQuery>();
        return services;
    }
}
