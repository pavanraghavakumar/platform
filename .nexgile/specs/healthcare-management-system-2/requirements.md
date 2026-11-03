# Requirements: Healthcare Management System

## Requirement 1: Sign in and role-based access

**User Story:** As a hospital user, I want to sign in with my own account and land on the dashboard of my role, so that I only see and change what my role permits.

_Source: R1, R2, R3, R4, BR2_

### Acceptance Criteria

1. THE SYSTEM SHALL offer four roles — Admin, Doctor, Receptionist and Patient — and give every signed-in user exactly one of them.
2. WHEN a person opens the address `/` without being signed in THE SYSTEM SHALL show the sign-in page.
3. WHEN a person signs in with a valid user name and password THE SYSTEM SHALL open the dashboard of that person's role: Admin, Doctor, Receptionist or Patient.
4. IF a person signs in with a wrong user name or password THEN THE SYSTEM SHALL keep the sign-in page open and show the message "Invalid user name or password."
5. WHEN a signed-in person clicks Sign out THE SYSTEM SHALL end the session and show the sign-in page.
6. IF a signed-in person opens a page their role may not use THEN THE SYSTEM SHALL show the message "You do not have access to this page." and keep the person signed in.
7. WHEN a signed-in person opens their profile page THE SYSTEM SHALL show their user name, email, role and phone number, and let them save a new phone number.
8. WHEN a signed-in person submits a new password with their current password THE SYSTEM SHALL save the new password and show the message "Your password has been changed."
9. IF a signed-in person submits a new password shorter than 8 characters THEN THE SYSTEM SHALL keep the old password and show the message "Password must be at least 8 characters."
10. WHEN a person submits their email on the forgot-password page THE SYSTEM SHALL show a reset link on the page itself, and that link SHALL open a page where the person sets a new password.

## Requirement 2: Admin dashboard and system administration

**User Story:** As an Admin, I want one dashboard with the hospital's key figures and the tools to manage users, departments, staff and settings, so that I can run the whole system from one place.

_Source: R1, G1, E3, E4, F2_

### Acceptance Criteria

1. WHEN an Admin opens the Admin dashboard THE SYSTEM SHALL show the total number of patients, doctors, staff and departments, each read from the database.
2. WHEN an Admin opens the Admin dashboard THE SYSTEM SHALL show today's appointment count, the pending appointment count, the completed consultation count, total revenue collected and the total outstanding balance, each read from the database.
3. WHEN an Admin opens the Admin dashboard THE SYSTEM SHALL show a revenue chart of the last 6 months and an appointment trend chart of the last 7 days, each drawn from the database.
4. WHEN an Admin opens the Admin dashboard THE SYSTEM SHALL show the 5 most recent patient registrations and the 5 most recent appointments, newest first.
5. WHEN an Admin opens the Admin dashboard THE SYSTEM SHALL show a department list with the number of doctors in each department.
6. WHEN an Admin creates a department with a name THE SYSTEM SHALL save it with status Active and show it in the department list.
7. IF an Admin creates a department with a name that already exists THEN THE SYSTEM SHALL keep the form open and show the message "A department with this name already exists."
8. WHEN an Admin deactivates a department THE SYSTEM SHALL set its status to Inactive and keep its doctors and appointments.
9. WHEN an Admin creates a staff member with full name, email, phone, role, designation and department THE SYSTEM SHALL save the staff member with status Active and show the staff member in the staff list.
10. WHEN an Admin deactivates a staff member or a doctor THE SYSTEM SHALL set the status to Inactive and block that person's sign-in with the message "Your account is deactivated."

## Requirement 3: Patient management

**User Story:** As a Receptionist, I want to register patients and find them again with their history, so that every visit starts from the right patient record.

_Source: R3, E1, G1_

### Acceptance Criteria

1. WHEN a Receptionist saves a new patient with full name, date of birth, gender, phone, email, address and emergency contact name and phone THE SYSTEM SHALL create the patient with a patient ID of the form `P-00001`, the next number in sequence, and status Active.
2. IF a Receptionist saves a patient without a full name or without a phone number THEN THE SYSTEM SHALL keep the form open and show the message "Full name and phone number are required."
3. WHEN a Receptionist opens the patient list THE SYSTEM SHALL show each patient's patient ID, full name, phone, gender, status and registration date, 10 patients per page.
4. WHEN a Receptionist types a name, patient ID or phone number in the patient search box THE SYSTEM SHALL show only the patients whose name, patient ID or phone number contains that text.
5. WHEN a Receptionist opens a patient's profile THE SYSTEM SHALL show the patient's personal and contact details, emergency contact, medical history, allergies and status.
6. WHEN a Receptionist edits a patient's contact details or emergency contact THE SYSTEM SHALL save the changes and show the message "Patient details saved."
7. WHEN a Receptionist opens a patient's appointment history THE SYSTEM SHALL list that patient's appointments with doctor, date, time and status, newest first.
8. WHEN a Receptionist deactivates a patient THE SYSTEM SHALL set the patient's status to Inactive and keep the patient's appointments, medical records and invoices.

## Requirement 4: Doctor management

**User Story:** As an Admin, I want to register doctors with their specialization, department, fees and working schedule, so that appointments can be booked only in the hours a doctor works.

_Source: R1, E2, G1_

### Acceptance Criteria

1. WHEN an Admin saves a new doctor with full name, email, phone, specialization, department, qualifications, years of experience and consultation fee THE SYSTEM SHALL create the doctor with status Active and a sign-in account of the role Doctor.
2. WHEN an Admin sets a doctor's working schedule THE SYSTEM SHALL save, for each day of the week, whether the doctor works and the start and end time of the working hours.
3. IF an Admin saves a doctor's working hours where the end time is not after the start time THEN THE SYSTEM SHALL keep the form open and show the message "End time must be after start time."
4. WHEN an Admin opens the doctor list THE SYSTEM SHALL show each doctor's full name, specialization, department, consultation fee and status, and let the Admin filter the list by department.
5. WHEN an Admin edits a doctor's consultation fee THE SYSTEM SHALL save the new fee and use it on invoices created after the change.
6. WHEN an Admin opens a doctor's profile THE SYSTEM SHALL show the doctor's details, working schedule and appointment history, newest first.
7. WHEN an Admin deactivates a doctor THE SYSTEM SHALL set the doctor's status to Inactive and stop new appointments from being booked with that doctor.

## Requirement 5: Appointment booking and status workflow

**User Story:** As a Receptionist or a Patient, I want to book an appointment in a free slot and move it through check-in and consultation, so that the hospital knows who is coming and who is being seen.

_Source: W1, R3, R4, E5, BR1, BR3, BR4, NT1_

### Acceptance Criteria

1. WHEN a Receptionist or a Patient picks a doctor and a date THE SYSTEM SHALL show only the time slots inside that doctor's working hours on that day that have no appointment with that doctor in status Pending, Confirmed, Checked In or In Progress.
2. WHEN a Receptionist books an appointment for any patient, or a Patient books an appointment for themselves, THE SYSTEM SHALL create the appointment with status Pending and show the message "Appointment booked."
3. IF a booking is submitted for a doctor and time that already has an appointment in status Pending, Confirmed, Checked In or In Progress THEN THE SYSTEM SHALL not create the appointment and show the message "This time slot is already booked."
4. IF a Patient tries to book an appointment for another patient THEN THE SYSTEM SHALL not create the appointment and show the message "You can only book appointments for yourself."
5. WHEN a Receptionist confirms a Pending appointment THE SYSTEM SHALL set its status to Confirmed and add a notification for the patient.
6. WHEN a Receptionist checks in a Confirmed appointment THE SYSTEM SHALL set its status to Checked In and add a notification for the doctor.
7. WHEN a Doctor starts a consultation on a Checked In appointment THE SYSTEM SHALL set its status to In Progress.
8. WHEN a Doctor completes a consultation THE SYSTEM SHALL set the appointment's status to Completed.
9. WHEN a Receptionist or the Patient who owns the appointment cancels it THE SYSTEM SHALL set its status to Cancelled, free the time slot for booking at once and add a notification for the other party.
10. WHEN a Receptionist marks a Confirmed appointment as No Show THE SYSTEM SHALL set its status to No Show and free the time slot for booking.

## Requirement 6: Consultation and medical records

**User Story:** As a Doctor, I want to record symptoms, diagnosis, vitals and a treatment plan during a consultation, so that the patient's medical record grows with every visit.

_Source: W2, R2, E6, BR2, BR6_

### Acceptance Criteria

1. WHEN a Doctor opens a Checked In or In Progress appointment THE SYSTEM SHALL show the patient's details, allergies, medical history and the last 5 consultations with their diagnosis and date.
2. WHEN a Doctor saves a consultation with symptoms, diagnosis, blood pressure, temperature, pulse, weight, treatment plan, observations and follow-up recommendations THE SYSTEM SHALL create a medical record for that appointment with status Draft.
3. WHEN a Doctor finalizes a Draft medical record THE SYSTEM SHALL set its status to Final and show the message "Medical record finalized."
4. IF a Doctor saves a medical record without a diagnosis THEN THE SYSTEM SHALL keep the form open and show the message "Diagnosis is required."
5. WHEN a Doctor opens a patient's medical records THE SYSTEM SHALL list that patient's records with date, doctor, diagnosis and status, newest first, and let the Doctor open any record.
6. IF a Doctor opens the medical records of a patient who has no appointment with that Doctor THEN THE SYSTEM SHALL show the message "You do not have access to this page."
7. WHEN a Doctor edits a Final medical record THE SYSTEM SHALL save the change and keep the previous value, the new value, the Doctor's user name and the time of the change in the audit trail.

## Requirement 7: Prescriptions

**User Story:** As a Doctor, I want to prescribe medicines with dosage and instructions and print the prescription, so that the patient leaves with a clear written plan.

_Source: W2, R2, E7, BR6_

### Acceptance Criteria

1. WHEN a Doctor adds a medicine to a prescription with dosage, frequency, duration in days and instructions THE SYSTEM SHALL add the medicine as a line of the prescription.
2. WHEN a Doctor saves a prescription with at least one medicine THE SYSTEM SHALL create it with status Active, linked to the patient, the Doctor and the consultation.
3. IF a Doctor saves a prescription with no medicine THEN THE SYSTEM SHALL keep the form open and show the message "Add at least one medicine."
4. WHEN a Doctor opens a patient's prescription history THE SYSTEM SHALL list that patient's prescriptions with date, Doctor, number of medicines and status, newest first.
5. WHEN a Doctor or the Patient who owns it opens a prescription THE SYSTEM SHALL show a printable page with the hospital name, patient name and patient ID, Doctor name, date, each medicine with dosage, frequency, duration and instructions, and a Print button.
6. WHEN a Doctor sets a prescription's status to Completed or Cancelled THE SYSTEM SHALL save the new status and keep the previous value, the new value, the Doctor's user name and the time of the change in the audit trail.

## Requirement 8: Laboratory tests and results

**User Story:** As a Doctor, I want to request lab tests and read the results in the app, so that the diagnosis rests on the patient's own test results.

_Source: W2, R2, E9, BR2_

### Acceptance Criteria

1. WHEN an Admin adds a laboratory test with name, category, price and normal range THE SYSTEM SHALL save it in the laboratory test catalog.
2. WHEN a Doctor requests one or more laboratory tests for a patient during a consultation THE SYSTEM SHALL create one lab request per test with status Requested, linked to the patient, the Doctor and the appointment.
3. WHEN a Receptionist or an Admin opens the lab request list THE SYSTEM SHALL show each request's patient, test, requesting Doctor, date and status, and let them filter the list by status.
4. WHEN a Receptionist or an Admin sets a Requested lab request to In Progress THE SYSTEM SHALL save the new status.
5. WHEN a Receptionist or an Admin enters a result value and remarks on an In Progress lab request THE SYSTEM SHALL set its status to Completed and add a notification for the requesting Doctor.
6. WHEN the requesting Doctor opens a completed lab request THE SYSTEM SHALL show the test name, normal range, result value, remarks and the date the result was entered.
7. IF a Doctor opens a lab request that Doctor did not request THEN THE SYSTEM SHALL show the message "You do not have access to this page."
8. WHEN a Doctor or the Patient who owns it opens a completed lab request THE SYSTEM SHALL show a printable report with the hospital name, patient name and patient ID, test name, normal range, result value, remarks and date, and a Print button.

## Requirement 9: Pharmacy and medicine inventory

**User Story:** As an Admin, I want to keep the medicine stock with batches and expiry dates and see what is running low, so that the pharmacy never runs out of what patients need.

_Source: R1, E8, G1_

### Acceptance Criteria

1. WHEN an Admin adds a medicine with name, category, supplier, unit price, reorder level and stock quantity THE SYSTEM SHALL save it with status In Stock.
2. WHEN an Admin adds a medicine category or a supplier with a name THE SYSTEM SHALL save it and show it in the list of that kind.
3. WHEN an Admin records a stock-in of a medicine with batch number, quantity and expiry date THE SYSTEM SHALL add the quantity to the medicine's stock and save the batch with its expiry date.
4. WHEN an Admin records a stock-out of a medicine with quantity and reason THE SYSTEM SHALL subtract the quantity from the medicine's stock.
5. IF a stock-out quantity is more than the medicine's stock THEN THE SYSTEM SHALL not change the stock and show the message "Not enough stock available."
6. WHILE a medicine's stock is at or below its reorder level and above zero THE SYSTEM SHALL show its status as Low Stock.
7. WHILE a medicine's stock is zero THE SYSTEM SHALL show its status as Out of Stock.
8. WHILE a medicine's earliest batch expiry date is before today THE SYSTEM SHALL show its status as Expired.
9. WHEN an Admin opens the pharmacy inventory report THE SYSTEM SHALL list every medicine with name, category, stock quantity, status and earliest expiry date, and show the count of medicines in Low Stock and Out of Stock.

## Requirement 10: Billing and payments

**User Story:** As a Receptionist, I want to raise an invoice for a visit's charges, apply a discount and tax, and record payments until it is paid, so that the hospital knows what each patient owes.

_Source: W3, R3, E10, E11, BR5, BR6, NT1_

### Acceptance Criteria

1. WHEN a Receptionist generates an invoice for a patient THE SYSTEM SHALL add the consultation fee of the appointment's Doctor, the price of each completed lab request of that appointment and the price of each medicine line of that appointment's prescription as invoice lines.
2. WHEN a Receptionist adds an additional service charge with a description and amount THE SYSTEM SHALL add it as an invoice line.
3. WHEN a Receptionist sets a discount amount and a tax rate on an invoice THE SYSTEM SHALL compute the total as the sum of the lines, minus the discount, plus the tax rate applied to the sum of the lines minus the discount, and show the total to 2 decimal places.
4. WHEN a Receptionist saves an invoice with a total of 100.00, a discount of 0.00 and a tax rate of 0% THE SYSTEM SHALL set its status to Unpaid.
5. WHEN a Receptionist records a payment of 50.00 against an invoice of 100.00 THE SYSTEM SHALL save the payment with its method and date, set the invoice status to Partially Paid and show the outstanding balance 50.00.
6. WHEN payments against an invoice add up to its total THE SYSTEM SHALL set the invoice status to Paid and show the outstanding balance 0.00.
7. IF a Receptionist records a payment larger than the invoice's outstanding balance THEN THE SYSTEM SHALL not save the payment and show the message "Payment cannot be more than the outstanding balance."
8. WHEN a Receptionist sets a payment's status to Refunded THE SYSTEM SHALL subtract its amount from the invoice's paid amount, set the invoice status back to Unpaid or Partially Paid, and add a notification for the patient.
9. WHEN a Receptionist or the Patient who owns it opens an invoice THE SYSTEM SHALL show a printable page with the hospital name, invoice number, patient name and patient ID, each line with description and amount, the discount, the tax, the total, the paid amount, the outstanding balance and a Print button.
10. WHEN a Receptionist records a payment THE SYSTEM SHALL keep the invoice's previous status, the new status, the Receptionist's user name and the time of the change in the audit trail.

## Requirement 11: Reports and analytics

**User Story:** As an Admin, I want reports of registrations, appointments, departments, laboratory, pharmacy and revenue, so that I can see how the hospital is doing over a period I choose.

_Source: R1, G1, BR2_

### Acceptance Criteria

1. WHEN an Admin opens the reports page and picks a start date and an end date THE SYSTEM SHALL show the number of patients registered in that period and a table of those patients with patient ID, name and registration date.
2. WHEN an Admin opens the appointment report for a period THE SYSTEM SHALL show the number of appointments in that period grouped by status and a table of appointments with patient, doctor, date and status.
3. WHEN an Admin opens the department report for a period THE SYSTEM SHALL show, for each department, the number of doctors and the number of appointments in that period.
4. WHEN an Admin opens the doctor performance report for a period THE SYSTEM SHALL show, for each doctor, the number of completed consultations and the revenue of that doctor's invoices in that period.
5. WHEN an Admin opens the laboratory report for a period THE SYSTEM SHALL show the number of lab requests in that period grouped by status and the number of completed results.
6. WHEN an Admin opens the pharmacy stock report THE SYSTEM SHALL show every medicine with stock quantity, status and earliest expiry date, and the count of medicines in Low Stock and Out of Stock.
7. WHEN an Admin opens the revenue report for a period THE SYSTEM SHALL show the total payments recorded, the total refunded, the net revenue and the total outstanding balance, and a chart of net revenue per day for that period.
8. IF a signed-in person who is not an Admin opens the reports page THEN THE SYSTEM SHALL show the message "You do not have access to this page."

## Requirement 12: Doctor dashboard

**User Story:** As a Doctor, I want my day's appointments, my patients and my consultations in one place, so that I can move from one patient to the next without searching.

_Source: R2, G1, E5, NT1_

### Acceptance Criteria

1. WHEN a Doctor opens the Doctor dashboard THE SYSTEM SHALL show the number of that Doctor's appointments today, the number of Checked In appointments waiting, the number of Completed consultations today and the number of lab results completed today.
2. WHEN a Doctor opens the Doctor dashboard THE SYSTEM SHALL show today's appointments with time, patient name, patient ID and status, earliest first.
3. WHEN a Doctor opens the Doctor dashboard THE SYSTEM SHALL show the next 5 upcoming appointments after today with date, time, patient name and status.
4. WHEN a Doctor opens the Doctor dashboard THE SYSTEM SHALL show the 5 most recent notifications for that Doctor, newest first, and mark them as read when the Doctor opens the notification panel.
5. WHEN a Doctor opens the patient list THE SYSTEM SHALL show only the patients who have an appointment with that Doctor, with patient ID, name, last appointment date and status.
6. WHEN a Doctor clicks Start consultation on a Checked In appointment THE SYSTEM SHALL open the consultation page for that appointment.

## Requirement 13: Receptionist dashboard

**User Story:** As a Receptionist, I want to register patients, book and check in appointments and take payments from one screen, so that the front desk moves quickly.

_Source: R3, G1, W1, W3, NT1_

### Acceptance Criteria

1. WHEN a Receptionist opens the Receptionist dashboard THE SYSTEM SHALL show the number of appointments today, the number of Pending appointments, the number of Checked In appointments and the total payments recorded today.
2. WHEN a Receptionist opens the Receptionist dashboard THE SYSTEM SHALL show today's appointments with time, patient name, doctor name and status, earliest first, each with a Check in button when its status is Confirmed.
3. WHEN a Receptionist opens the Receptionist dashboard THE SYSTEM SHALL show a patient search box that opens the matching patient's profile when the Receptionist picks a result.
4. WHEN a Receptionist opens the Receptionist dashboard THE SYSTEM SHALL show the 5 most recent notifications for that Receptionist, newest first.
5. WHEN a Receptionist clicks New appointment on the Receptionist dashboard THE SYSTEM SHALL open the appointment booking page.
6. WHEN a Receptionist clicks New invoice on the Receptionist dashboard THE SYSTEM SHALL open the invoice page for the patient the Receptionist picks.

## Requirement 14: Patient dashboard and patient portal

**User Story:** As a Patient, I want to book and cancel my own appointments and read my records, prescriptions, lab reports and invoices, so that I can follow my care without calling the hospital.

_Source: R4, BR2, BR3, BR4, NT1, G1_

### Acceptance Criteria

1. WHEN a Patient opens the Patient dashboard THE SYSTEM SHALL show that Patient's next appointment with doctor, date, time and status, and the number of upcoming appointments.
2. WHEN a Patient opens the Patient dashboard THE SYSTEM SHALL show the number of that Patient's unpaid invoices and the total outstanding balance.
3. WHEN a Patient opens the Patient dashboard THE SYSTEM SHALL show the 5 most recent notifications for that Patient, newest first.
4. WHEN a Patient clicks Book appointment THE SYSTEM SHALL show the list of Active doctors with specialization, department and consultation fee, and let the Patient pick a doctor and a free time slot.
5. WHEN a Patient opens My appointments THE SYSTEM SHALL list that Patient's appointments with doctor, date, time and status, newest first, each with a Cancel button when its status is Pending or Confirmed.
6. WHEN a Patient clicks Cancel on a Pending or Confirmed appointment THE SYSTEM SHALL ask for confirmation, then set its status to Cancelled and free the time slot at once.
7. WHEN a Patient opens My records THE SYSTEM SHALL list that Patient's Final medical records with date, doctor and diagnosis, and let the Patient open any record.
8. WHEN a Patient opens My prescriptions, My lab reports or My invoices THE SYSTEM SHALL list only that Patient's own prescriptions, completed lab requests and invoices, each opening its printable page.
9. IF a Patient opens the record, prescription, lab report or invoice of another patient THEN THE SYSTEM SHALL show the message "You do not have access to this page."

## Requirement 15: Notifications

**User Story:** As a patient or a staff member, I want to be told inside the app when my appointment or my work changes, so that I do not miss what happens next.

_Source: NT1, R1, R2, R3, R4_

### Acceptance Criteria

1. WHEN an event adds a notification for a user THE SYSTEM SHALL show the number of that user's unread notifications on the bell icon of the top navigation bar.
2. WHEN a user clicks the bell icon THE SYSTEM SHALL open a panel with that user's 10 most recent notifications, newest first, each with its message and time.
3. WHEN a user opens the notification panel THE SYSTEM SHALL mark that user's notifications as read and set the unread count on the bell icon to 0.
4. IF a user opens the notification panel with no notifications THEN THE SYSTEM SHALL show the message "You have no notifications."
5. THE SYSTEM SHALL send no email and no SMS for any notification.

## Requirement 16: Audit trail

**User Story:** As an Admin, I want to see who changed which medical or financial record and when, so that every change can be traced back to a person.

_Source: BR6, R1_

### Acceptance Criteria

1. WHEN a user saves a change to a medical record, a prescription, an invoice or a payment THE SYSTEM SHALL save an audit entry with the record type, the record ID, the field changed, the previous value, the new value, the user name and the time of the change.
2. WHEN an Admin opens the audit trail page THE SYSTEM SHALL list audit entries newest first, 20 per page, with record type, record ID, field, previous value, new value, user name and time.
3. WHEN an Admin filters the audit trail by record type and by a start date and an end date THE SYSTEM SHALL show only the entries of that record type in that period.
4. IF a user opens a record without changing it THEN THE SYSTEM SHALL save no audit entry.

## Requirement 17: Look, layout and responsiveness

**User Story:** As a hospital user, I want a polished, consistent interface that works on a desktop, a tablet and a phone, so that the system feels like a professional hospital product.

_Source: F1, S2, R1, R2, R3, R4_

### Acceptance Criteria

1. THE SYSTEM SHALL show every signed-in page inside one layout with a collapsible sidebar, a top navigation bar with the hospital name, a search box, a notification bell and a user profile dropdown, a breadcrumb and a footer.
2. THE SYSTEM SHALL style every page with one design system of navy blue, white, teal and soft grey, rounded cards with subtle shadows, one icon set and one font, and no page SHALL show the browser's default form controls.
3. WHEN a user clicks the sidebar toggle THE SYSTEM SHALL collapse the sidebar to icons and expand it again on the next click.
4. WHEN a user opens a list page THE SYSTEM SHALL show a search box, a sort control, a status filter, pagination and a status badge on each row.
5. WHEN a user clicks a delete or cancel action THE SYSTEM SHALL show a confirmation dialog before the action runs.
6. WHEN a form is submitted with an invalid value THE SYSTEM SHALL show the error message beside the field and keep the values the user typed.
7. WHEN a list has no rows THE SYSTEM SHALL show the message "No records found."
8. WHILE the browser window is 375 pixels wide THE SYSTEM SHALL show every page without horizontal scrolling, with the sidebar hidden behind a menu button.
9. WHILE the browser window is 1280 pixels wide THE SYSTEM SHALL show the sidebar expanded beside the content area.
10. WHILE the sidebar navigation or the main content is taller than the browser window THE SYSTEM SHALL let the user scroll either area independently without moving the other area.

## Requirement 18: Demo data and demo logins

**User Story:** As a person trying the system, I want sample records and a login for each role, so that I can see every dashboard working on the first run.

_Source: F2, R1, R2, R3, R4_

### Acceptance Criteria

1. WHEN the app starts on an empty database THE SYSTEM SHALL create the demo records: at least 4 departments, 4 doctors, 3 staff members, 10 patients, 12 appointments across all 7 statuses, 6 medical records, 5 prescriptions, 6 lab requests, 10 medicines, 8 invoices and 8 payments.
2. WHEN the app starts on an empty database THE SYSTEM SHALL create one demo login for each role — Admin, Doctor, Receptionist and Patient — and each SHALL sign in and open its own dashboard.
3. WHEN the app starts on a database that already holds records THE SYSTEM SHALL keep the existing records and create no demo record.

## Later releases

- Hosting and deployment: not relevant, the user said so [out of scope]

## Assumptions

- The hospital is one hospital: the app holds one hospital name and one set of departments, and has no multi-site or multi-tenant support.
- A doctor's working schedule is the same every week, one start and end time for each day of the week, and appointments are booked in 30-minute slots.
- A Patient's sign-in account is created by the Receptionist when the patient is registered, with the patient's email as the user name and a password the Receptionist sets.
- A Patient may cancel only their own appointment, and only while it is Pending or Confirmed; a Cancelled or No Show appointment cannot be reopened.
- A consultation is one medical record per appointment, and a prescription belongs to one consultation.
- Lab requests are entered and completed by the Receptionist or the Admin, since the brief names no laboratory role.
- An invoice is generated for one appointment, and its lines come from that appointment's consultation fee, completed lab requests and prescription medicines.
- The tax rate is a percentage the Receptionist enters on each invoice, and the discount is an amount in the hospital's currency.
- The hospital's currency is the Indian rupee, shown with the symbol ₹ and 2 decimal places.
- The forgot-password reset link is shown on the page itself because the app sends no email.
- The audit trail covers medical records, prescriptions, invoices and payments, the records the brief names as medical or financial.
- The demo logins are for local use only, and their passwords are listed in the design.
