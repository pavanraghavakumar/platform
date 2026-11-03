# Requirements brief — Healthcare Management System
## Profile
An enterprise healthcare management system for hospitals to manage patients, doctors, staff, appointments, consultations, medical records, prescriptions, laboratory, pharmacy, billing, payments and reports, with role-based dashboards for Admin, Doctor, Receptionist and Patient. · internal tool · Healthcare / Hospital Management · size: large · depth: thorough · money, personalData, regulated: health, files, existingData, notifies
- **Has a user interface?** Yes (The description specifies a full web UI with dashboards, forms and tables.)
- **Runs locally?** Yes (It is an ASP.NET Core MVC web app that runs locally with SQLite.)
## The app
An enterprise healthcare management system for hospitals to manage patients, doctors, staff, appointments, consultations, medical records, prescriptions, laboratory, pharmacy, billing, payments and reports, with role-based dashboards for Admin, Doctor, Receptionist and Patient.
## Ruled out by the person
- OUT1 cloud hosting or deployment: "The application must run locally without cloud hosting, deployment, or external database servers." (description) [no requirement, design, task or code for it]
## Goals and success
- G1 Provide a centralized platform for managing hospital operations, including patients, doctors, staff, appointments, consultations, medical records, prescriptions, laboratory services, pharmacy inventory, billing, and payments. (description)
## Roles
- R1 Admin (description)
- R2 Doctor (description)
- R3 Receptionist (description)
- R4 Patient (description)
## Permissions
- R4 Patient may book an appointment, cancel an appointment (Q1, Q2)
- R2 Doctor may request a lab test, view lab results (Q5)
## Workflows
### W1 Appointment booking (release 1) — actors: R3 Receptionist, R4 Patient; trigger: Patient requests an appointment; outcome: Appointment completed or cancelled; module: Appointments (description, Q1)
1. Receptionist or patient selects doctor and available slot
2. App checks doctor availability and prevents conflicts
3. Appointment is created with status Pending
4. Receptionist confirms appointment
5. Patient checks in on arrival
6. Doctor starts consultation
- Doctor unavailable
- Double booking
### W2 Consultation and prescription (release 1) — actors: R2 Doctor; trigger: Patient checked in; outcome: Medical record and prescription saved; module: Clinical (description, Q5)
1. Doctor views patient history
2. Doctor records symptoms, diagnosis, vitals
3. Doctor creates prescription
4. Doctor requests lab tests and later views the results in the app
5. Doctor completes consultation
- Patient no-show
- Missing history
### W3 Billing and payment (release 1) — actors: R3 Receptionist; trigger: Services rendered; outcome: Invoice paid or outstanding; module: Billing (description)
1. Receptionist generates invoice with charges
2. App applies discounts and taxes
3. Receptionist records payment
4. App updates invoice status
5. Receipt printed
- Partial payment
- Refund
## Records
- E1 Patient. fields: Patient ID, personal and contact information, emergency contact details, medical history, allergies; states: Active → Inactive; owner: Receptionist (description)
- E2 Doctor. fields: specialization, department, qualifications, experience, consultation fees, availability and working schedules, status; states: Active → Inactive; owner: Admin (description)
- E3 Staff. fields: role, designation, department; states: Active → Inactive; owner: Admin (description)
- E4 Department. fields: name, status; states: Active → Inactive; owner: Admin (description)
- E5 Appointment. fields: patient, doctor, date and time, status; states: Pending → Confirmed → Checked In → In Progress → Completed → Cancelled → No Show; owner: Receptionist (description)
- E6 MedicalRecord. fields: patient, consultation notes, symptoms, diagnosis, vital signs, treatment plan, doctor observations, follow-up recommendations; states: Draft → Final; owner: Doctor (description)
- E7 Prescription. fields: patient, doctor, medicines, dosage, frequency, duration, instructions; states: Active → Completed → Cancelled; owner: Doctor (description)
- E8 Medicine. fields: name, category, supplier, stock quantity, batch, expiry date; states: In Stock → Low Stock → Out of Stock → Expired; owner: Admin (description)
- E9 LabRequest. fields: patient, doctor, test, status, result; states: Requested → In Progress → Completed → Cancelled; owner: Doctor (description)
- E10 Invoice. fields: patient, items, discounts, taxes, total, status; states: Unpaid → Partially Paid → Paid → Cancelled; owner: Receptionist (description)
- E11 Payment. fields: invoice, amount, method, date; states: Recorded → Refunded; owner: Receptionist (description)
## Rules
- BR1 Prevent conflicting appointment bookings for the same doctor at the same time.. Applies: Doctor already has an appointment at 10:00; booking another at 10:00 is blocked.. Does not apply: Doctor has no appointment at 10:00; booking at 10:00 is allowed. (description)
- BR2 Each role accesses only its permitted modules and information.. Applies: A patient cannot view another patient's medical records.. Does not apply: An admin can view all records. (description)
- BR3 Patients may book their own appointments in the app, as well as receptionists.. Applies: A patient books an appointment for themselves.. Does not apply: A patient books an appointment for another patient. (Q1)
- BR4 When a patient cancels an appointment, the slot is freed immediately for others to book.. Applies: A patient cancels a 10:00 appointment; the 10:00 slot becomes bookable at once.. Does not apply: A patient cancels an appointment for next week; the slot was already free. (Q2)
- BR5 Invoices may be paid in part, and the outstanding balance is tracked until paid in full.. Applies: A patient pays half of a 100 invoice; the invoice shows 50 outstanding.. Does not apply: A patient pays the full 100 invoice; the invoice is Paid. (Q4)
- BR6 Every change to a medical or financial record is kept in an audit trail with who changed it and when.. Applies: A doctor edits a diagnosis; the old and new values and the doctor are recorded.. Does not apply: A user opens a record without changing it; nothing is recorded. (Q6)
## Notifications
- NT1 Patients and staff are told inside the app of appointment changes and other updates; no email or SMS is sent. (Q3)
## Scope and constraints
- S1 Must run locally without cloud hosting, deployment, or external database servers. [constraint] (description)
- S2 Technology stack: ASP.NET Core MVC, C#, Entity Framework Core, SQLite, Bootstrap 5, Chart.js. [constraint] (description)
## Decisions
- **Stack:** The stack the design part of the spec recommends for this app (decided by Greenfield) (Q9)
- **Deploy target:** No deploy for now (as the user's description or documents say, description: "The application must run locally without cloud hosting, deployment, or external database servers.") (Q7)
## Other facts
- F1 The app must have a premium, modern, professional healthcare SaaS interface with a custom design system, not default Bootstrap. [scope] (description)
- F2 The app must include sample/demo records and demo login credentials for all four roles. [migration] (description)
## Out of scope and later releases
- Hosting and deployment: not relevant, the user said so [out of scope]
## The interview
- Q1 [permissions · owner] Should patients be able to book their own appointments in the app, or only receptionists book them? → Both can book
- Q2 [workflows · owner] When a patient cancels an appointment, should the app free the slot immediately for others to book? → Yes, free the slot immediately
- Q3 [notifications · owner] Should the app send notifications to patients and staff, or only show them inside the app? → Only inside the app
- Q4 [money · owner] Should the app handle partial payments and outstanding balances? → Yes, track partial payments and balances
- Q5 [workflows · owner] Should doctors be able to request lab tests and view results in the app? → Yes, request and view
- Q6 [audit · owner] Should the app keep an audit trail of who changed medical and financial records? → Yes, for all changes
- Q8 [success · owner] When the hospital looks back after the first months of using the app, what would tell them it is working well? → Decide for me
## Decided by Greenfield (Enough, decide the rest)
- Q9 Which languages and frameworks should the app use? → Decide for me (settled: The stack the design part of the spec recommends for this app)
