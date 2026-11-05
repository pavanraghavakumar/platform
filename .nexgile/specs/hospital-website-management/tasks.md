# Tasks: Premium Hospital Website – Healthcare Management System

- [x] 1. Project setup for ASP.NET Core MVC hospital website
  - Create the .NET 8 solution and four-layer project skeleton, preserve-compatible EF Core SQLite foundation, public Razor layout, design tokens, secure middleware defaults, `/health`, development seed and settings, test projects, and README commands from the design.
  - Add `@playwright/test` as a development dependency in `package.json`, create the `e2e/` journey folder with `e2e/.gitkeep`, and configure the unit test runner to exclude `e2e/` so Greenfield's Tester can save journeys for `run_journeys` without unit tests collecting them.
  - Establish reusable application contracts, result types, temporary-SQLite integration fixture, fake clock/email/monitoring adapters, loopback `PORT` binding, and one passing architecture and health test so later features can be added without editing shared bootstrap files.
  - _Requirements: 1.7, 1.8, 12.2, 12.9_
  - _Files: HospitalWebsiteManagement.sln, README.md, .gitignore, .env.example, Directory.Build.props, src/Hospital.Web/Hospital.Web.csproj, src/Hospital.Web/Program.cs, src/Hospital.Web/Configuration/**, src/Hospital.Web/Middleware/**, src/Hospital.Web/Views/Shared/**, src/Hospital.Web/ViewComponents/**, src/Hospital.Web/wwwroot/css/site.css, src/Hospital.Web/wwwroot/js/site.js, src/Hospital.Web/appsettings.json, src/Hospital.Web/appsettings.Development.json, src/Hospital.Application/Hospital.Application.csproj, src/Hospital.Application/Contracts/**, src/Hospital.Application/Common/**, src/Hospital.Domain/Hospital.Domain.csproj, src/Hospital.Domain/Models/**, src/Hospital.Domain/Rules/**, src/Hospital.Infrastructure/Hospital.Infrastructure.csproj, src/Hospital.Infrastructure/Data/**, src/Hospital.Infrastructure/Identity/**, src/Hospital.Infrastructure/Messaging/**, src/Hospital.Infrastructure/Monitoring/**, tests/Hospital.UnitTests/Hospital.UnitTests.csproj, tests/Hospital.UnitTests/Setup/**, tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj, tests/Hospital.IntegrationTests/Fixtures/**, tests/Hospital.IntegrationTests/Setup/**, tests/Hospital.ArchitectureTests/**, e2e/.gitkeep, playwright.config.ts, package.json, package-lock.json_
  - _Verify: dotnet test tests/Hospital.ArchitectureTests/Hospital.ArchitectureTests.csproj --filter Setup_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter Health_
  - _Area: setup_
  - _Issue: #48_

- [ ] 2. Public hospital homepage and navigation
  - Build the premium public homepage at `/` with the responsive hospital header, accessible desktop and off-canvas navigation, healthcare hero, all approved editorial/card sections, contact band, and full footer using published projections and maintained contact fallbacks.
  - Add responsive, accessibility, navigation, composition, and representative performance-budget tests; use page-specific styling plus the shared navy, teal, white, typography, focus, motion, and spacing tokens.
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/HomeController.cs, src/Hospital.Web/ViewModels/Home/**, src/Hospital.Web/Views/Home/**, src/Hospital.Web/wwwroot/css/pages/home.css, src/Hospital.Web/wwwroot/js/pages/home.js, src/Hospital.Application/Features/Home/**, src/Hospital.Infrastructure/PublicContent/Home/**, tests/Hospital.UnitTests/Home/**, tests/Hospital.IntegrationTests/Home/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter Home_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter Home_
  - _Area: ui_
  - _Issue: #49_

- [ ] 3. About Hospital page
  - Build the `/about` editorial screen with hospital introduction, professional imagery, experience and achievement cards, quality-of-care content, safe omission of missing optional items, and visible routes to Departments, Our Doctors, Medical Services, and Book an Appointment.
  - Include finished phone, tablet, and desktop styling and rendering tests for content, links, missing optional data, semantics, and responsive media behavior.
  - _Requirements: 2.1, 2.2, 2.3_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/AboutController.cs, src/Hospital.Web/ViewModels/About/**, src/Hospital.Web/Views/About/**, src/Hospital.Web/wwwroot/css/pages/about.css, src/Hospital.Application/Features/About/**, src/Hospital.Infrastructure/PublicContent/About/**, tests/Hospital.UnitTests/About/**, tests/Hospital.IntegrationTests/About/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter About_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter About_
  - _Area: ui_
  - _Issue: #50_

- [x] 4. Departments and Department Details
  - Build `/departments` as an image-led responsive card grid and `/departments/{slug}` as a polished detail screen with related published Doctors and a Department-preselected Book an Appointment route.
  - Integrate publication changes through the existing data boundary, return the branded public not-found state for unavailable records, and test lists, details, links, filtering of publication states, and update visibility.
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/DepartmentsController.cs, src/Hospital.Web/ViewModels/Departments/**, src/Hospital.Web/Views/Departments/**, src/Hospital.Web/wwwroot/css/pages/departments.css, src/Hospital.Application/Features/Departments/**, src/Hospital.Infrastructure/PublicContent/Departments/**, tests/Hospital.UnitTests/Departments/**, tests/Hospital.IntegrationTests/Departments/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter Departments_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter Departments_
  - _Area: ui_
  - _Issue: #51_

- [ ] 5. Our Doctors, Doctor Details, and availability
  - Build `/doctors` with Department and specialization filters and professional profile cards, plus `/doctors/{slug}` with profile, qualifications, Department, accessible availability states, retry behavior, and a Doctor-preselected Book an Appointment action.
  - Query only published Doctors, use the existing scheduling service as the availability source, return branded not-found results, reflect approved profile changes, and test HTML, query filters, service states, and the three-second availability target instrumentation.
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/DoctorsController.cs, src/Hospital.Web/ViewModels/Doctors/**, src/Hospital.Web/Views/Doctors/**, src/Hospital.Web/wwwroot/css/pages/doctors.css, src/Hospital.Web/wwwroot/js/pages/doctors.js, src/Hospital.Application/Features/Doctors/**, src/Hospital.Infrastructure/PublicContent/Doctors/**, tests/Hospital.UnitTests/Doctors/**, tests/Hospital.IntegrationTests/Doctors/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter Doctors_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter Doctors_
  - _Area: ui_
  - _Issue: #52_

- [ ] 6. Medical Services and Service Details
  - Build `/medical-services` with healthcare image cards and `/medical-services/{slug}` with an editorial service presentation, relevant care information, and a prominent Book an Appointment route.
  - Apply publication-state projections, branded not-found handling, responsive page-specific styling, and tests proving public updates flow through while unavailable records remain hidden.
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/MedicalServicesController.cs, src/Hospital.Web/ViewModels/MedicalServices/**, src/Hospital.Web/Views/MedicalServices/**, src/Hospital.Web/wwwroot/css/pages/medical-services.css, src/Hospital.Application/Features/MedicalServices/**, src/Hospital.Infrastructure/PublicContent/MedicalServices/**, tests/Hospital.UnitTests/MedicalServices/**, tests/Hospital.IntegrationTests/MedicalServices/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter MedicalServices_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter MedicalServices_
  - _Area: ui_
  - _Issue: #53_

- [ ] 7. Health Packages information screens
  - Build `/health-packages` and `/health-packages/{slug}` as polished, responsive information-only card and detail screens with no booking, checkout, purchase, payment, card, charge, tax, refund, or invoice controls.
  - Apply publication-state changes and branded not-found handling, and test both rendered controls and public contracts to preserve the existing billing boundary.
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/HealthPackagesController.cs, src/Hospital.Web/ViewModels/HealthPackages/**, src/Hospital.Web/Views/HealthPackages/**, src/Hospital.Web/wwwroot/css/pages/health-packages.css, src/Hospital.Application/Features/HealthPackages/**, src/Hospital.Infrastructure/PublicContent/HealthPackages/**, tests/Hospital.UnitTests/HealthPackages/**, tests/Hospital.IntegrationTests/HealthPackages/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter HealthPackages_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter HealthPackages_
  - _Area: ui_
  - _Issue: #54_

- [ ] 8. Appointment Booking journey and selection APIs
  - Build the complete `/appointments/book` stepper and the Department, Doctor, availability, slot, and `POST /appointments` contracts, using visual Doctor and time-slot cards, accessible validation, responsive summaries, and deterministic dependent-selection clearing.
  - Revalidate all selections server-side, invoke the existing appointment management service once with an idempotency key, preserve its immediate status and Patient actions, queue confirmation email, and implement truthful validation, slot-conflict, continuation, service-failure, and retry states with integration and concurrency tests.
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 7.10_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/AppointmentController.cs, src/Hospital.Web/Controllers/Api/PublicAppointmentSelectionController.cs, src/Hospital.Web/ViewModels/Appointments/**, src/Hospital.Web/Views/Appointment/**, src/Hospital.Web/wwwroot/css/pages/appointment-booking.css, src/Hospital.Web/wwwroot/js/pages/appointment-booking.js, src/Hospital.Application/Features/Appointments/**, src/Hospital.Infrastructure/Appointments/**, src/Hospital.Infrastructure/Messaging/AppointmentMessages/**, tests/Hospital.UnitTests/Appointments/**, tests/Hospital.IntegrationTests/Appointments/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter Appointments_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter Appointments_
  - _Area: ui_
  - _Issue: #55_

- [ ] 9. Contact Us and shared reception queue
  - Build `/contact` with the responsive contact methods, map, Emergency Care route, consent-aware enquiry form, field validation, progress, success, and retry states, then implement idempotent `POST /contact` queueing.
  - Create one Contact enquiry and one shared reception queue item transactionally, issue the Receptionist alert, add the one-business-day escalation worker and queue report counts, and test failure truthfulness and repeated worker/submission behavior.
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/ContactController.cs, src/Hospital.Web/ViewModels/Contact/**, src/Hospital.Web/Views/Contact/**, src/Hospital.Web/wwwroot/css/pages/contact.css, src/Hospital.Web/wwwroot/js/pages/contact.js, src/Hospital.Application/Features/Contact/**, src/Hospital.Infrastructure/ReceptionQueue/**, tests/Hospital.UnitTests/Contact/**, tests/Hospital.IntegrationTests/Contact/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter Contact_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter Contact_
  - _Area: ui_
  - _Issue: #56_

- [ ] 10. Patient Registration, Patient Login, and secured patient landing
  - Build focused public-layout Registration and Patient Login cards with the exact authentication fields, password reveal behavior, linked validation summaries, generic credential errors, safe value redisplay, and links between journeys.
  - Adapt the existing authentication service for account creation and sign-in, protect `/patient`, validate local continuation tokens, resume Appointment Booking at the stored step, and test existing-account, invalid-credential, unavailable-service, and safe redirect outcomes.
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/PatientAuthenticationController.cs, src/Hospital.Web/Controllers/PatientController.cs, src/Hospital.Web/ViewModels/PatientAuthentication/**, src/Hospital.Web/Views/PatientAuthentication/**, src/Hospital.Web/Views/Patient/**, src/Hospital.Web/wwwroot/css/pages/patient-authentication.css, src/Hospital.Web/wwwroot/js/pages/patient-authentication.js, src/Hospital.Application/Features/PatientAuthentication/**, src/Hospital.Infrastructure/Identity/PatientAuthentication/**, tests/Hospital.UnitTests/PatientAuthentication/**, tests/Hospital.IntegrationTests/PatientAuthentication/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter PatientAuthentication_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter PatientAuthentication_
  - _Area: ui_
  - _Issue: #57_

- [ ] 11. Emergency Care instructions and operational review
  - Build `/emergency-care` with the emergency phone, immediate-life-threatening-emergency statement, contact and location instructions first, plus finished responsive styling and a persistent route supplied by the public shell.
  - Use maintained fallback configuration when current content fails, audit authorized instruction changes, create idempotent monthly Doctor availability and Emergency Care review items, and test current, fallback, audit, and repeated-worker outcomes.
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/EmergencyCareController.cs, src/Hospital.Web/ViewModels/EmergencyCare/**, src/Hospital.Web/Views/EmergencyCare/**, src/Hospital.Web/wwwroot/css/pages/emergency-care.css, src/Hospital.Application/Features/EmergencyCare/**, src/Hospital.Infrastructure/OperationalReview/**, tests/Hospital.UnitTests/EmergencyCare/**, tests/Hospital.IntegrationTests/EmergencyCare/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter EmergencyCare_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter EmergencyCare_
  - _Area: ui_
  - _Issue: #58_

- [ ] 12. Patient privacy requests, retention, and testimonial consent
  - Implement authenticated `POST /patient/privacy-requests` and the protected processing workflow for identity verification, affected-record discovery, correction, deletion or anonymization, retention decisions, outcomes, responsible Admin, and immutable audit evidence.
  - Enforce explicit Patient Testimonial consent and public withdrawal, and test access denial before verification, unverifiable identity, seven-year retention, eligible changes, required audit history, completion records, and consent transitions.
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9, 11.10_
  - _Depends: 1_
  - _Files: src/Hospital.Web/Controllers/PrivacyRequestsController.cs, src/Hospital.Web/ViewModels/PrivacyRequests/**, src/Hospital.Application/Features/PrivacyRequests/**, src/Hospital.Application/Features/TestimonialConsent/**, src/Hospital.Domain/Rules/Privacy/**, src/Hospital.Infrastructure/Privacy/**, src/Hospital.Infrastructure/Auditing/Privacy/**, tests/Hospital.UnitTests/PrivacyRequests/**, tests/Hospital.IntegrationTests/PrivacyRequests/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter PrivacyRequests_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter PrivacyRequests_
  - _Area: api_
  - _Issue: #59_

- [ ] 13. Protected access, immutable audit, and healthcare workflow continuity
  - Apply deny-by-default policies and object-level authorization to preserved appointment scheduling, billing, pharmacy, laboratory, medical-record, access, and public-emergency operations while keeping their existing models, services, workflows, accounts, and SQLite records authoritative.
  - Add immutable protected-view/change audit evidence and redacted Support diagnostics without impersonation, then test role matrices, public-route denial, domain-service invocation, audit retention, redaction, and compatibility against a representative existing database fixture.
  - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 12.9_
  - _Depends: 1_
  - _Files: src/Hospital.Application/Features/ProtectedHealthcare/**, src/Hospital.Application/Features/SupportDiagnostics/**, src/Hospital.Infrastructure/Authorization/**, src/Hospital.Infrastructure/Auditing/ProtectedActivity/**, src/Hospital.Infrastructure/Monitoring/SupportDiagnostics/**, tests/Hospital.UnitTests/ProtectedHealthcare/**, tests/Hospital.IntegrationTests/ProtectedHealthcare/**, tests/Hospital.IntegrationTests/Compatibility/**, tests/Hospital.IntegrationTests/Fixtures/representative-existing.sqlite_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter ProtectedHealthcare_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter ProtectedHealthcare_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter Compatibility_
  - _Area: api_
  - _Issue: #60_

- [ ] 14. Appointment communications, reporting, and operational alerts
  - Implement idempotent 24-hour Appointment reminders, booking funnel and 90-day completion measurement, Department-and-Doctor demand and slot reporting, and deduplicated safe IT support alerts for website, database, authentication, Appointment service, and reception queue failures.
  - Add authorization, period selection, zero-start handling, email/monitoring adapters, and fake-clock integration tests proving report calculations, single reminder effects, and protected-content-free alert payloads.
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_
  - _Depends: 1, 8, 9_
  - _Files: src/Hospital.Web/Controllers/OperationalReportsController.cs, src/Hospital.Web/ViewModels/OperationalReports/**, src/Hospital.Web/Views/OperationalReports/**, src/Hospital.Web/wwwroot/css/pages/operational-reports.css, src/Hospital.Application/Features/AppointmentCommunications/**, src/Hospital.Application/Features/Reporting/**, src/Hospital.Infrastructure/Jobs/AppointmentReminders/**, src/Hospital.Infrastructure/Reporting/**, src/Hospital.Infrastructure/Monitoring/OperationalAlerts/**, tests/Hospital.UnitTests/OperationalReporting/**, tests/Hospital.IntegrationTests/OperationalReporting/**_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter OperationalReporting_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter OperationalReporting_
  - _Area: api_
  - _Issue: #61_

- [ ] 15. Capacity, data-volume, resilience, and recovery verification
  - Add repeatable load and data-volume fixtures for 500 public visitors, 75 signed-in people, 50,000 Patient records, 250,000 historical Appointments, and the year-1/year-3 Appointment volumes, measuring the approved public, availability, and submission percentiles without bypassing existing workflows.
  - Add fault, backup, migration-copy, cutover, restore, persistent-path, WAL, contention, and availability-evidence checks that exercise staging-compatible data and produce machine-readable results for the 99.9%, four-hour, and 15-minute operational gates.
  - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 14.8_
  - _Depends: 1, 8, 9, 10, 13_
  - _Files: tests/Hospital.LoadTests/**, tests/Hospital.ResilienceTests/**, tests/Hospital.IntegrationTests/Capacity/**, tests/Hospital.IntegrationTests/Recovery/**, tools/TestDataBuilder/**, docs/operations/capacity-and-recovery.md_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter Capacity_
  - _Verify: dotnet test tests/Hospital.ResilienceTests/Hospital.ResilienceTests.csproj --filter Recovery_
  - _Verify: dotnet test tests/Hospital.LoadTests/Hospital.LoadTests.csproj --filter ExpectedLoad_
  - _Area: setup_
  - _Issue: #62_

- [ ]* 16. Property tests for P1 published public information
  - Generate mixed publication states for About Hospital, Departments, Doctors, Medical Services, and Health Packages and prove every list, detail, filter, and update projection exposes only published information and returns the public not-found result otherwise.
  - _Properties: P1_
  - _Requirements: 2.3, 3.1, 3.2, 3.4, 3.5, 4.1, 4.2, 4.3, 4.6, 4.7, 5.1, 5.2, 5.4, 5.5, 6.1, 6.2, 6.5, 6.6_
  - _Depends: 3, 4, 5, 6, 7_
  - _Files: tests/Hospital.UnitTests/Properties/PublishedPublicInformationProperties.cs_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter PublishedPublicInformationProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #63_

- [ ]* 17. Property tests for P2 and P13 public privacy and role authorization
  - Generate protected values, roles, routes, and object ownership combinations to prove public projections exclude protected data and existing role policies alone authorize protected healthcare actions.
  - _Properties: P2, P13_
  - _Requirements: 12.1, 12.2, 12.3, 12.7, 12.8_
  - _Depends: 13_
  - _Files: tests/Hospital.UnitTests/Properties/PublicPrivacyAndAuthorizationProperties.cs_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter PublicPrivacyAndAuthorizationProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #64_

- [ ]* 18. Property tests for P3–P5 Appointment consistency
  - Generate selection transitions, repeated valid submissions, and lost-slot races to prove dependent choices stay consistent, accepted retries remain idempotent, and an unavailable slot never creates an Appointment.
  - _Properties: P3, P4, P5_
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.7, 7.8, 7.9, 12.4_
  - _Depends: 8_
  - _Files: tests/Hospital.UnitTests/Properties/AppointmentConsistencyProperties.cs, tests/Hospital.IntegrationTests/Properties/AppointmentAtomicityProperties.cs_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter AppointmentConsistencyProperties_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter AppointmentAtomicityProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #65_

- [ ]* 19. Property tests for P6 and P7 truthful form failures and exact queueing
  - Generate invalid and unavailable-service outcomes plus repeated Contact submissions to prove failed forms never claim success and accepted enquiries create exactly one enquiry, queue item, and Receptionist alert.
  - _Properties: P6, P7_
  - _Requirements: 7.6, 7.10, 8.2, 8.3, 8.4, 8.5, 9.2, 9.7_
  - _Depends: 8, 9, 10_
  - _Files: tests/Hospital.IntegrationTests/Properties/TruthfulSubmissionProperties.cs, tests/Hospital.IntegrationTests/Properties/ContactQueueingProperties.cs_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter TruthfulSubmissionProperties_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter ContactQueueingProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #66_

- [ ]* 20. Property tests for P8, P20, and P21 timed operations and safe fallbacks
  - Generate times, worker replays, Emergency Care read failures, and operational dependency failures to prove one effect per due occurrence, maintained emergency fallback output, and one deduplicated protected-content-free support alert.
  - _Properties: P8, P20, P21_
  - _Requirements: 8.6, 10.3, 10.5, 13.1, 13.5_
  - _Depends: 9, 11, 14_
  - _Files: tests/Hospital.UnitTests/Properties/TimedOperationsProperties.cs, tests/Hospital.IntegrationTests/Properties/SafeFallbackAndAlertProperties.cs_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter TimedOperationsProperties_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter SafeFallbackAndAlertProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #70_

- [ ]* 21. Property tests for P9 and P10 Patient authentication
  - Generate account existence, invalid credentials, and valid, absent, altered, or external continuations to prove generic failures create no session and successful authentication returns only to a safe stable destination.
  - _Properties: P9, P10_
  - _Requirements: 9.1, 9.3, 9.4, 9.5, 9.6, 9.8, 9.9_
  - _Depends: 10_
  - _Files: tests/Hospital.UnitTests/Properties/PatientAuthenticationProperties.cs_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter PatientAuthenticationProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #67_

- [ ]* 22. Property tests for P11 and P12 privacy and consent
  - Generate Privacy requests, linked records, retention decisions, and Patient Testimonial consent histories to prove verification gates every action, eligible and retained data follow their rules, outcomes are audited, and public visibility follows current explicit consent.
  - _Properties: P11, P12_
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9, 11.10_
  - _Depends: 12_
  - _Files: tests/Hospital.UnitTests/Properties/PrivacyAndConsentProperties.cs_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter PrivacyAndConsentProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #68_

- [ ]* 23. Property tests for P14 immutable protected audit evidence
  - Generate protected views, changes, actors, and ordinary-user mutation attempts to prove complete audit evidence is appended once and cannot be altered or deleted during retention.
  - _Properties: P14_
  - _Requirements: 10.4, 12.5, 12.6_
  - _Depends: 11, 13_
  - _Files: tests/Hospital.UnitTests/Properties/ProtectedAuditProperties.cs_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter ProtectedAuditProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #69_

- [ ]* 24. Property tests for P15 operational report totals
  - Generate booking, slot, and Contact enquiry events over selected periods to prove all grouped and overall totals agree with source events and the nonzero booking-start completion percentage uses valid unassisted submissions.
  - _Properties: P15_
  - _Requirements: 8.7, 13.2, 13.3, 13.4_
  - _Depends: 14_
  - _Files: tests/Hospital.UnitTests/Properties/OperationalReportProperties.cs_
  - _Verify: dotnet test tests/Hospital.UnitTests/Hospital.UnitTests.csproj --filter OperationalReportProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #71_

- [ ]* 25. Property tests for P16 and P17 migration preservation and information-only packages
  - Generate representative existing SQLite fixtures and published Health Package renderings to prove migrations preserve every protected identifier and value while public package controls and contracts never introduce booking or money operations.
  - _Properties: P16, P17_
  - _Requirements: 6.3, 6.4, 12.9, 14.2, 14.6, 14.7, 14.8_
  - _Depends: 7, 13, 15_
  - _Files: tests/Hospital.IntegrationTests/Properties/MigrationAndPackageBoundaryProperties.cs_
  - _Verify: dotnet test tests/Hospital.IntegrationTests/Hospital.IntegrationTests.csproj --filter MigrationAndPackageBoundaryProperties_
  - _Mode: testgen_
  - _Area: setup_
  - _Issue: #72_

- [ ] 26. Deployment setup for Azure App Service
  - Create Azure App Service staging and production deployment infrastructure, application package and pipeline, persistent mounts for SQLite, data-protection keys and media, health checks, restricted settings, monitoring, pre-cutover backup, 15-minute backup schedule, restore runbook, and safe promotion with one production system of record.
  - Run migrations before startup, create the first production Admin only from `FIRST_ADMIN_EMAIL` and `FIRST_ADMIN_PASSWORD` when no account exists, never run development seed in Production, keep secrets in App Service settings, and add static deployment validation that needs no Azure credentials.
  - _Requirements: 13.5, 14.3, 14.4, 14.5, 14.6, 14.7, 14.8_
  - _Depends: 15_
  - _Files: .github/workflows/azure-app-service.yml, deploy/azure/**, src/Hospital.Web/Deployment/**, docs/operations/azure-app-service.md, tests/Hospital.ArchitectureTests/Deployment/**_
  - _Verify: dotnet test tests/Hospital.ArchitectureTests/Hospital.ArchitectureTests.csproj --filter Deployment_
  - _Mode: devops_
  - _Area: deploy_
  - _Issue: #73_

## Assumptions

- Existing implementation names will be mapped into the task-owned feature paths when they differ, as settled in the approved design.
