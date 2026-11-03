import { test, expect } from "@playwright/test"

const patientEmail = process.env.E2E_PATIENT_EMAIL ?? "patient.ravi@hms.local"
const patientPassword = process.env.E2E_PATIENT_PASSWORD ?? "Patient@123"

test("patient uses dashboard, booking, and own portal pages", async ({ page }) => {
  await page.goto("/Account/Login")
  await page.getByLabel("User name").fill(patientEmail)
  await page.getByLabel("Password").fill(patientPassword)
  await page.getByRole("button", { name: "Sign in" }).click()

  await expect(page).toHaveURL(/\/Dashboard\/Patient/)
  await expect(page.getByRole("heading", { name: "Patient dashboard" })).toBeVisible()
  await expect(page.getByText("Upcoming appointments", { exact: true })).toBeVisible()
  await expect(page.getByText("Unpaid invoices", { exact: true })).toBeVisible()
  await expect(page.getByText("Outstanding balance", { exact: true })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Recent notifications" })).toBeVisible()

  await page.getByRole("main").getByRole("link", { name: /Book appointment/ }).click()
  await expect(page.getByRole("heading", { name: "Book appointment" })).toBeVisible()
  await expect(page.getByRole("radio", { name: /Dr\. Arjun Mehta Cardiology.*₹800\.00/ })).toBeVisible()
  await page.getByRole("radio", { name: /Dr\. Arjun Mehta Cardiology/ }).check()
  const future = new Date()
  future.setDate(future.getDate() + (future.getDay() === 5 ? 3 : future.getDay() === 6 ? 2 : 1))
  const date = future.toISOString().slice(0, 10)
  await page.getByLabel("Appointment date").fill(date)
  await expect(page.getByLabel("Free time slot").locator("option")).toHaveCount(17)
  await expect(page.getByLabel("Free time slot").locator("option", { hasText: "09:00" })).toHaveCount(1)
  await expect(page.getByLabel("Free time slot").locator("option", { hasText: "16:30" })).toHaveCount(1)

  await page.goto("/Portal/Appointments")
  await expect(page.getByRole("heading", { name: "My appointments" })).toBeVisible()
  const cancel = page.getByRole("button", { name: "Cancel" }).first()
  if (await cancel.count()) {
    page.once("dialog", async dialog => {
      expect(dialog.message()).toBe("Cancel this appointment?")
      await dialog.dismiss()
    })
    await cancel.click()
  }

  await page.goto("/Portal/Records")
  await expect(page.getByRole("heading", { name: "My records" })).toBeVisible()
  await expect(page.getByRole("link", { name: "Open record" }).first()).toHaveAttribute("href", /\/MedicalRecord\/Details\//)

  await page.goto("/Portal/Prescriptions")
  await expect(page.getByRole("heading", { name: "My prescriptions" })).toBeVisible()
  await expect(page.getByRole("link", { name: "Open printable prescription" }).first()).toHaveAttribute("href", /\/Prescription\/Print\//)

  await page.goto("/Portal/LabReports")
  await expect(page.getByRole("heading", { name: "My lab reports" })).toBeVisible()
  await expect(page.getByRole("link", { name: "Open printable report" }).first()).toHaveAttribute("href", /\/LabRequest\/Print\//)

  await page.goto("/Portal/Invoices")
  await expect(page.getByRole("heading", { name: "My invoices" })).toBeVisible()
  await expect(page.getByRole("link", { name: "Open printable invoice" }).first()).toHaveAttribute("href", /\/Invoice\/Print\//)
})
