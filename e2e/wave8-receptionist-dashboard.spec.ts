import { test, expect } from "@playwright/test"

test("receptionist dashboard exposes KPIs, appointments, search and quick actions", async ({ page }) => {
  await page.goto("/")
  await page.getByRole("textbox", { name: "User name" }).fill(process.env.E2E_RECEPTIONIST_EMAIL ?? "reception@hms.local")
  await page.getByRole("textbox", { name: "Password" }).fill(process.env.E2E_RECEPTIONIST_PASSWORD ?? "Reception@123")
  await page.getByRole("button", { name: "Sign in" }).click()
  await page.goto("/Dashboard/Receptionist")

  for (const label of ["Appointments today", "Pending appointments", "Checked In appointments", "Payments recorded today"]) {
    await expect(page.getByText(label, { exact: true })).toBeVisible()
  }
  await expect(page.getByRole("heading", { name: "Today's appointments" })).toBeVisible()
  for (const heading of ["Time", "Patient", "Doctor", "Status"]) {
    await expect(page.getByRole("columnheader", { name: heading })).toBeVisible()
  }
  await expect(page.getByRole("heading", { name: "Recent notifications" })).toBeVisible()
  await expect(page.getByLabel("Patient").first()).toBeVisible()

  const appointment = page.getByRole("link", { name: "New appointment" })
  await expect(appointment).toHaveAttribute("href", "/Appointment/Book")
  await appointment.click()
  await expect(page).toHaveURL(/\/Appointment\/Book$/)
  await page.goto("/Dashboard/Receptionist")
  const patient = page.locator("#invoicePatient")
  await patient.selectOption({ index: 1 })
  await expect(page.getByRole("button", { name: "New invoice" })).toBeEnabled()
})
