# Requirements brief — Hospital Website Management
## Profile
A public-facing hospital website for patients and visitors that retains the existing healthcare management features used by administrators, doctors, and receptionists. · public hospital website with an internal management system · hospital and healthcare management · size: large · depth: thorough · money, personalData, regulated: health, publicUsers, existingData, notifies
- **Has a user interface?** Yes (The project specifies a public hospital website, patient pages, forms, navigation, login, registration, and a separate administration interface.)
- **Runs locally?** Yes (The ASP.NET Core MVC application can serve its public and secured pages on the person's machine using the local SQLite database and local stand-ins for email and monitoring.)
## The app
The application is a responsive public hospital website where visitors can explore departments, doctors, services, packages, health information, emergency instructions, and contact details, while patients can register, sign in, view scheduling-service availability, and submit appointment requests through the existing workflow. It preserves the secured Admin, Doctor, Receptionist, scheduling, billing, pharmacy, laboratory, and medical-record functionality, with role-based access, healthcare privacy controls, audit records, operational monitoring, and an Azure App Service deployment using persistent storage and backups for the existing SQLite database.
## Ruled out by the person
- OUT1 an admin portal as the primary public interface: "NOT an admin portal, ERP dashboard, or generic management application." (description) [no requirement, design, task or code for it]
## Goals and success
- G1 Transform the existing Healthcare Management System into a modern public-facing hospital website rather than an admin-style portal. (description)
- G2 Give patients a simple way to find departments, doctors, services, health packages, and available appointment slots and then submit an appointment request. (description)
- G3 Preserve the existing database, authentication, appointment scheduling, billing, pharmacy, laboratory, medical record functionality, and backend business logic. (description)
- M1 By 90 days after launch, at least 70% of patients who start appointment booking submit a valid request without staff help. (At least 70% booking completion by 90 days after launch) [assumed] (Q8, assumption)
## Roles
- R1 Registers or logs in, browses hospital information, checks doctor availability, and requests appointments. — Patient (About 10,000 registered patients in year 1) [assumed] (description, assumption)
- R2 Browses hospital, department, doctor, service, health package, article, and contact information without using the internal management interface. — Public website visitor (About 25,000 distinct visitors in a typical month in year 1) [never signs in to the app] [assumed] (description, assumption)
- R3 Uses the existing secured administration and management features. — Admin (5 administrators) [assumed] (description, assumption)
- R4 Uses the existing secured doctor management features. — Doctor (100 doctors) [assumed] (description, assumption)
- R5 Uses the existing secured receptionist and appointment management features. — Receptionist (15 receptionists) [assumed] (description, assumption)
## Permissions
- R1 Patient may view health package details (Q5)
## Business events
- EV1 A patient submits an appointment request. (description)
- EV2 A visitor submits the hospital contact form. (description)
- EV3 A visitor looks for emergency-care help. (Q6)
- EV4 A patient registers or signs in to the patient area. [assumed] (description, assumption)
- EV5 An appointment is due within 24 hours and the existing appointment workflow still shows it as active. [assumed] (assumption)
- EV6 A contact enquiry has remained unhandled for one business day. [assumed] (assumption)
- EV7 At the start of each month, public doctor availability and emergency contact information are due for review. [assumed] (assumption)
## Workflows
### W1 Book an appointment (release 1) — actors: R1 Patient; trigger: A patient chooses to book an appointment.; outcome: The appointment request is stored through the existing database and appointment management services, the existing appointment workflow determines its immediate status, and the patient receives confirmation that it was submitted.; how often: About 100 appointment requests on a typical day and up to 250 on a peak day; module: Public website [assumed] (description, Q2, assumption)
1. The patient selects a department.
2. The patient selects a doctor.
3. The website shows the doctor's availability.
4. The patient selects an appointment date.
5. The patient chooses an available time slot.
6. The patient enters personal information.
7. The website validates the entered information.
8. The patient submits the appointment request.
9. The website provides booking confirmation.
- X1 A selected slot is no longer available when the patient submits the request. [assumed, high impact] (assumption)
### W2 Browse hospital information (release 1) — actors: R1 Patient, R2 Public website visitor; trigger: A person opens the public hospital website.; outcome: The person finds relevant hospital information without entering the internal management interface.; how often: Continuous, with about 25,000 distinct visitors in a typical month; module: Public website [assumed] (description, assumption)
1. The person opens the homepage or navigation.
2. The person chooses hospital, department, doctor, service, health package, article, or contact information.
3. The website displays the corresponding public information.
4. The person can continue browsing or start an appointment request.
- X2 A public information record is missing, unpublished, or no longer available. [assumed] (assumption)
### W3 Send a contact enquiry (release 1) — actors: R1 Patient, R2 Public website visitor; trigger: A person opens the contact form.; outcome: The submitted contact enquiry is placed in a shared reception queue.; how often: About 20 contact enquiries on a typical day; module: Public website [assumed] (description, Q7, assumption)
1. The person enters contact information and a message.
2. The website validates the information.
3. The person submits the contact form.
4. The website stores or sends the enquiry.
- X3 The contact enquiry cannot be placed in the shared reception queue. [assumed] (assumption)
### W4 Find emergency-care instructions (release 1) — actors: R1 Patient, R2 Public website visitor; trigger: A person looks for emergency-care help on the public website.; outcome: The person is directed to the hospital's emergency-care contact route.; how often: A few times each day, with possible peaks during local emergencies; module: Public website [assumed] (Q6, assumption)
1. The person opens the emergency-care information.
2. The website shows the hospital's emergency contact instructions.
3. The person uses those instructions to contact or reach emergency care.
- X4 Emergency-care information cannot be loaded. [assumed, high impact] (assumption)
### W5 Register or sign in as a patient (release 1) — actors: R1 Patient; trigger: A patient chooses Registration or Patient Login.; outcome: The patient has a secured account session governed by the existing authentication service.; how often: About 100 registrations and 500 patient logins per week in year 1; module: Public website [assumed] (description, assumption)
1. The patient opens registration or login.
2. For registration, the patient enters the required account and personal information.
3. The website checks the information on the server and in the browser.
4. The website creates the account or verifies the login credentials.
5. The patient enters the patient area or returns to the appointment journey.
- Existing account
- Invalid credentials
- Unavailable authentication service
- X5 Registration uses an existing account or login credentials are invalid. [assumed, high impact] (assumption)
### W6 Handle a privacy request (release 1) — actors: R1 Patient, R3 Admin; trigger: A person asks to access, correct, or delete their personal information.; outcome: The privacy request is completed with legally required records protected from improper deletion.; how often: Fewer than 20 requests per month in year 1; module: Internal management [assumed, high impact] (assumption)
1. The request is recorded.
2. Admin verifies the requester's identity.
3. The system identifies the person's account, appointment, enquiry, billing, and medical records.
4. Admin provides or corrects eligible information.
5. The system deletes or anonymizes eligible information and retains records required by health, billing, or audit rules.
6. The outcome and responsible administrator are recorded.
- Identity cannot be verified
- A retention rule prevents deletion
## Records
- E1 Appointment. fields: department, doctor, doctor availability, appointment date, time slot, patient personal information, booking confirmation; states: Draft while the patient enters details → Submitted when validation succeeds → Pending or Confirmed as decided by the existing appointment workflow → Completed, Cancelled, or No-show as decided by the existing workflow; owner: The existing appointment management service, with reception responsible for operational handling; how many: About 36,500 new appointments in year 1 and 55,000 in year 3; kept for: Seven years after the appointment unless the applicable health-record rule requires longer [assumed, high impact] (description, Q2, assumption)
- E2 Department. fields: name, image, description, department details; states: Draft → Published → Archived; owner: Admin; how many: About 25 active departments; kept for: Active records indefinitely and archived versions for seven years [assumed, high impact] (description, assumption)
- E3 Doctor. fields: name, photo, qualifications, specialization, department, availability, doctor details; states: Draft → Published → Unavailable → Archived; owner: Admin, with availability supplied by the existing scheduling service; how many: About 100 active doctors in year 1 and 150 in year 3; kept for: While employed or affiliated, then archived for seven years [assumed, high impact] (description, Q3, assumption)
- E4 Medical service. fields: name, description, service details; states: Draft → Published → Archived; owner: Admin; how many: About 80 active medical services; kept for: Active records indefinitely and archived versions for seven years [assumed, high impact] (description, assumption)
- E5 Health package. fields: name, description, package details; states: Draft → Published → Withdrawn → Archived; owner: Admin; how many: About 20 active health packages; kept for: Active records indefinitely and archived versions for seven years [assumed, high impact] (description, Q5, assumption)
- E6 Patient account. fields: registration information, login credentials, personal information; states: Pending registration → Active → Locked → Closed; owner: Patient, with security controls managed by Admin; how many: About 10,000 accounts in year 1 and 30,000 in year 3; kept for: While active and for seven years after closure where linked to care or billing [assumed, high impact] (description, assumption)
- E7 Patient testimonial. fields: patient feedback, review; states: Draft → Approved → Published → Withdrawn; owner: Admin; how many: About 50 published testimonials; kept for: Until consent is withdrawn, then retained only as required for the consent audit [assumed, high impact] (description, assumption)
- E8 Health article. fields: healthcare tip, medical information; states: Draft → Published → Archived; owner: Admin; how many: About 100 articles in year 1 and 300 in year 3; kept for: Published versions indefinitely and superseded versions for seven years [assumed, high impact] (description, assumption)
- E9 Contact enquiry. fields: contact information, message; states: New → Assigned → Answered → Closed; owner: Receptionist; how many: About 7,300 enquiries in year 1 and 11,000 in year 3; kept for: Two years after closure unless it becomes part of a patient record [assumed, high impact] (description, Q7, assumption)
## Rules
- BR1 Internal management features require secure authentication and role-based authorization.. Applies: An Admin, Doctor, or Receptionist opens or changes internal management information.. Does not apply: A visitor reads public hospital, doctor, department, service, package, article, or contact information. (description)
- BR2 Information entered through forms must be checked on both the server and in the browser.. Applies: A patient submits appointment, login, registration, or contact information.. Does not apply: A visitor reads a public information page without submitting information. (description)
- BR3 Patients must use a simple public hospital website rather than being forced into the internal management dashboard.. Applies: A patient browses services or books an appointment.. Does not apply: An authorized staff member uses an internal management feature. (description)
- BR4 Existing appointment workflows and backend business logic must be preserved.. Applies: The public website submits an appointment through the existing appointment management services.. Does not apply: A change concerns only public hospital information that has no existing backend workflow. (description)
- BR5 Health packages are provided online for information only.. Applies: A patient or visitor reads the details of a health package.. Does not apply: A patient tries to book or pay for a health package online. (Q5)
- BR6 The public website does not take payment for appointment requests or health packages; charges, refunds, taxes, and invoices remain in the existing internal billing workflow.. Applies: A patient submits an appointment request or reads a health package online.. Does not apply: Authorized staff process a bill, tax, invoice, or refund through the preserved billing functionality. [assumed, high impact] (description, Q5, assumption)
- BR7 Public visitors must never see patient, medical record, billing, private appointment, or staff account information.. Applies: A visitor opens a public page or changes an address to a protected route.. Does not apply: An authorized staff member opens information allowed for that role after signing in. [assumed, high impact] (description, assumption)
- BR8 The hospital's local privacy and health-record laws govern the data; care, appointment, billing, and audit records are retained for seven years unless local law requires longer, while marketing or testimonial use requires explicit consent.. Applies: Patient information is used for care, booking, billing, a testimonial, or an account deletion request.. Does not apply: A person reads a public page without providing personal information. [assumed, high impact] (assumption)
- BR9 Support may unlock an account or reproduce a public problem but may not impersonate a patient or clinical user to view protected records.. Applies: Support investigates a login failure or public website error.. Does not apply: A clinical or billing decision requires access to a patient's protected record. [assumed, high impact] (assumption)
## Quality targets
- N1 [scale] Support 500 simultaneous public visitors and 75 simultaneous signed-in staff or patients at the busiest moment.: 575 simultaneous people [assumed] (assumption)
- N2 [scale] Expect the busiest periods on weekday mornings and after public campaigns, with about 36,500 new appointments in year 1 and 55,000 in year 3.: 36,500 year-1 appointments and 55,000 year-3 appointments appointment records [assumed] (assumption)
- N3 [performance] Public information pages should display their main content within two seconds for at least 95% of requests under expected load.: 2 seconds at the 95th percentile [assumed] (description, assumption)
- N4 [performance] Doctor availability should appear within three seconds and appointment submission should return a result within five seconds for at least 95% of requests.: 3 seconds for availability and 5 seconds for submission [assumed] (description, Q3, assumption)
- N5 [availability] The public website and appointment booking should be available 99.9% each calendar month, excluding announced maintenance.: 99.9 percent monthly availability [assumed, high impact] (assumption)
- N6 [availability] After a serious outage, service should return within four hours and no more than 15 minutes of confirmed data may be lost.: 4-hour recovery time and 15-minute data-loss limit [assumed, high impact] (assumption)
- N7 [access_i18n] Public pages, forms, navigation, validation messages, and sign-in flows should meet WCAG 2.2 AA.: WCAG 2.2 AA accessibility level [assumed] (assumption)
## Notifications
- NT1 The patient is shown an on-screen result and is sent an email when an email address is provided after an appointment request is accepted by the existing appointment workflow; this transactional message cannot be turned off. [assumed] (description, Q2, assumption)
- NT2 Receptionists see a new item in the shared reception queue when a contact enquiry is submitted; this operational alert cannot be turned off for the queue. [assumed] (Q7, assumption)
- NT3 The patient is sent an appointment reminder by email 24 hours before an active appointment when an email address is available. [assumed] (assumption)
- NT4 The reception team sees an escalation in the shared queue when a contact enquiry has remained unhandled for one business day. [assumed] (assumption)
## Reports
- RP1 Appointment booking funnel report showing visits, booking starts, validation failures, and submitted requests. [assumed] (Q8, assumption)
- RP2 Appointment demand and slot report by department and doctor. [assumed] (Q3, assumption)
- RP3 Contact enquiry queue report showing new, assigned, overdue, answered, and closed enquiries. [assumed] (Q7, assumption)
## Scope and constraints
- S1 Use ASP.NET Core MVC in C#, Entity Framework Core, SQLite, HTML, CSS, Bootstrap 5, and JavaScript. [constraint] (description)
- S2 Build the public pages for Home, About Hospital, Departments, Department Details, Doctors, Doctor Details, Medical Services, Service Details, Appointment Booking, Health Packages, Contact Us, Login, and Registration. (description)
- S3 Keep the existing Admin, Doctor, and Receptionist management features working securely in a separate administration interface. (description)
- S4 Reuse the existing models, controllers, services, database, authentication, authorization, appointment workflows, and backend business logic. [constraint] (description)
- S6 Preserve the existing appointment scheduling, billing, pharmacy, laboratory, and medical record functionality. (description)
- S7 Deploy the application to Azure App Service with persistent storage suitable for the existing SQLite database, separate staging and production environments, the hospital's public domain, restricted staff routes, and automated backups. [constraint] [assumed, high impact] (assumption)
## Decisions
- **Stack:** ASP.NET Core MVC (C#), Entity Framework Core, SQLite, HTML, CSS, Bootstrap 5, and JavaScript (as the user's description or documents say, description: "Transform the existing Healthcare Management System into a modern, premium, fully responsive hospital website using ASP.NET Core MVC (C#), Entity Framework Core, SQLite, HTML, CSS, Bootstrap 5, and JavaScript.") (Q1)
- **Deploy target:** Azure App Service (decided by Greenfield; suggested: It fits ASP.NET Core, supports separate staging and production environments, and reduces operational work while preserving the existing application and database.) (Q9)
## Assumptions
- A1 The website will be considered successful if, by 90 days after launch, at least 70% of patients who start appointment booking submit a valid request without staff help. — This directly measures whether the new public booking experience is simpler for patients while allowing time to collect a useful sample. [assumed] (assumption, Q8)
## Risks
- K1 The worst likely misuse is unauthorized access to medical records, billing information, appointment details, or staff management functions. [assumed, high impact] (assumption)
- K2 Changes to public appointment booking could bypass or break the existing scheduling rules. [assumed, high impact] (description, Q2, Q3, Q4, assumption)
- K3 Published doctor availability or emergency contact instructions could be stale or incorrect. [assumed, high impact] (Q3, Q6, assumption)
- K4 The public launch could expose protected healthcare or staff functions through missing authorization checks. [assumed, high impact] (description, assumption)
- K5 SQLite write contention or hosting storage configuration could fail under peak appointment and staff activity. [assumed, high impact] (description, assumption)
## Other facts
- F1 The primary interface is a professional public-facing hospital website with a separate internal administration interface. [scope] (description)
- F2 The public website must support responsive desktop, tablet, and mobile layouts. [devices] (description)
- F3 The homepage includes hospital introduction, specialties, doctors, medical services, reasons to choose the hospital, appointment booking, health packages, testimonials, health articles, contact information, and a footer. [scope] (description)
- F4 The public navigation includes Home, About Us, Departments, Our Doctors, Medical Services, Appointments, Health Packages, Contact Us, Patient Login, and a Book an Appointment action. [scope] (description)
- F5 Responsive layouts and application performance must be optimized. [performance] (description)
- F6 The existing scheduling service controls the appointment slots shown to patients. [permissions] (Q3)
- F7 After booking, patients can use the appointment actions already provided by the existing appointment workflow. [workflows] (Q4)
- F8 Patients and public website visitors are affected if doctor availability, emergency instructions, or hospital information is inaccurate; receptionists could lose out if the public forms create duplicate or incomplete work. [stakeholders] [assumed] (assumption)
- F9 Today the existing Healthcare Management System holds appointments and supports staff workflows, while the patient-facing experience does not provide the required commercial hospital website journey. [current] [assumed] (description, assumption)
- F10 The main current difficulties are finding hospital information, checking availability, and reaching appointment booking without encountering a staff-oriented experience. [current] [assumed] (description, assumption)
- F11 Current data is held in the existing SQLite database and is handled by the existing ASP.NET Core MVC models, controllers, services, authentication, and appointment workflow. [current] [assumed] (description, assumption)
- F12 Mayo Clinic and Cleveland Clinic public websites are the assumed closest references: copy their clear service discovery, doctor information, and prominent appointment routes while avoiding dense navigation and unnecessary steps before booking. [similar] [assumed] (assumption)
- F13 Authorized administrators can change departments, doctor public profiles, services, packages, articles, testimonials, contact details, emergency instructions, and public availability settings without a developer. [admin] [assumed] (assumption)
- F14 The application serves one hospital organisation, so public settings are hospital-wide rather than separate per customer; department-specific details and schedules may differ by department. [admin] [assumed] (assumption)
- F15 The existing billing system remains the source of prices, invoices, taxes, and refunds; it uses one hospital-configured local currency, and card details do not pass through the public website. [money] [assumed, high impact] (description, Q5, assumption)
- F16 An hour of outage can prevent appointment requests and delay staff access, so it is treated as a high operational and patient-service cost; planned maintenance occurs outside local clinic hours with advance notice. [availability] [assumed, high impact] (assumption)
- F17 Patients sign in with an email address or patient identifier and password; Admin, Doctor, and Receptionist accounts use the existing authentication with a second verification step. [security] [assumed, high impact] (description, assumption)
- F18 The application holds patient identity and contact details, login information, appointment details, medical records, billing information, contact enquiries, and optional testimonial consent for users in the hospital's launch country. [privacy] [assumed, high impact] (description, assumption)
- F19 The audit trail must prove who viewed or changed protected patient, appointment, billing, pharmacy, laboratory, medical record, access, and public emergency information, what changed, and when it happened. [audit] [assumed, high impact] (description, assumption)
- F20 Security and clinical audit entries are kept for seven years and cannot be changed by ordinary users. [audit] [assumed, high impact] (assumption)
- F21 The first release uses English, left-to-right text, the hospital's local time zone, and the single local currency configured in the existing billing system. [access_i18n] [assumed] (assumption)
- F22 Patients and visitors use desktop, tablet, and phone browsers, including phones away from the hospital; staff use supported desktop and tablet browsers in connected hospital locations. [devices] [assumed] (description, assumption)
- F23 The application requires a connection; when the connection is lost it must not claim a form was submitted, and the person retries after service returns. [devices] [assumed] (assumption)
- F24 The hospital's IT support team is alerted by email and its monitoring console when the website, database, authentication, appointment service, or reception queue fails. [operations] [assumed, high impact] (assumption)
- F25 Support can see service health, failed requests, queue delays, application logs, audit references, and account status without seeing medical content unless separately authorized. [operations] [assumed, high impact] (assumption)
- F26 The existing SQLite database, models, authentication accounts, appointments, billing, pharmacy, laboratory, and medical records remain in place rather than being transformed into a new data model. [migration] [assumed, high impact] (description, assumption)
- F27 Planning assumes about 50,000 patient records and 250,000 historical appointment records at cutover; a tested copy is used in staging, and the production database is backed up immediately before launch. [migration] [assumed, high impact] (assumption)
- F28 Cutover occurs on the public website launch date; staging and production run side by side for testing, but there is only one live system of record after launch. [migration] [assumed, high impact] (assumption)
## Out of scope and later releases
- S5 Do not make an admin portal, ERP dashboard, or generic management application the primary interface. [out of scope] (description)
- Scope and constraints: not relevant, the user said so [out of scope]
## The interview
- Q2 [workflows · owner] What should an appointment request mean immediately after a patient submits it? → The existing appointment workflow decides
- Q3 [permissions · owner] Who should control the appointment slots shown to patients? → The existing scheduling service controls them
- Q4 [workflows · owner] What should patients be able to do with an appointment after booking? → Use the existing appointment actions
- Q5 [money · owner] What should patients be able to do with health packages online? → Read package details only
- Q6 [workflows · owner] How should an emergency-care visitor be directed? → Show emergency contact instructions
- Q7 [notifications · owner] Where should a submitted contact form go? → A shared reception queue
- Q8 [success · owner] What would tell you that the new public hospital website is succeeding for patients after launch? → Decide for me (settled: The website will be considered successful if, by 90 days after launch, at least 70% of patients who start appointment booking submit a valid request without staff help.)
## Decided by Greenfield (Enough, decide the rest)
- Q9 Where should the public hospital website and preserved management features be deployed? → Decide for me (settled: Azure App Service)
