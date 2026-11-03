import { test, expect } from "@playwright/test"

const receptionistEmail = process.env.E2E_RECEPTIONIST_EMAIL ?? "reception@hms.local"
const receptionistPassword = process.env.E2E_RECEPTIONIST_PASSWORD ?? "Reception@123"

test("receptionist books a uniquely identified available appointment", async ({ page }) => {
  await page.goto("/Account/Login")
  await page.getByLabel("User name").fill(receptionistEmail)
  await page.getByLabel("Password").fill(receptionistPassword)
  await page.getByRole("button", { name: "Sign in" }).click()

  await page.goto("/Appointment/Book")
  await page.getByLabel("Patient").selectOption({ label: "P-00001 - Ravi Kumar" })
  await page.getByLabel("Doctor").selectOption({ label: "Dr. Arjun Mehta - Cardiology" })

  const appointmentDate = nextWorkingDate()
  await page.getByLabel("Date").fill(appointmentDate)

  const timeSelect = page.getByLabel("Time slot")
  await expect(timeSelect.locator("option")).not.toHaveCount(1)
  const selectedTime = await timeSelect.locator("option").nth(1).getAttribute("value")
  expect(selectedTime).toMatch(/^\d{2}:\d{2}$/)
  await timeSelect.selectOption(selectedTime!)
  await page.getByRole("button", { name: "Book", exact: true }).click()

  await expect(page.getByText("Appointment booked.", { exact: true })).toBeVisible()

  const displayedDate = formatDisplayDate(appointmentDate)
  const newAppointment = page.locator("tbody tr").filter({
    has: page.locator(`td[data-label="Date"]:text-is("${displayedDate}")`),
  }).filter({
    has: page.locator(`td[data-label="Time"]:text-is("${selectedTime}")`),
  }).filter({
    has: page.locator('td[data-label="Patient"]:text-is("Ravi Kumar")'),
  }).filter({
    has: page.locator('td[data-label="Doctor"]:text-is("Dr. Arjun Mehta")'),
  }).filter({
    has: page.locator('td[data-label="Status"]:has-text("Pending")'),
  })

  try {
    await expect(newAppointment).toHaveCount(1)
    await expect(newAppointment.getByText("Pending", { exact: true })).toBeVisible()
  } finally {
    if (await newAppointment.count() === 1) {
      await newAppointment.getByRole("button", { name: "Cancel", exact: true }).click()
      await page.locator("#hmsConfirmDialog").getByRole("button", { name: "Confirm", exact: true }).click()
      await expect(page.getByText("Appointment cancelled.", { exact: true })).toBeVisible()
    }
  }
})

function nextWorkingDate(): string {
  const date = new Date()
  date.setDate(date.getDate() + 1)
  while (date.getDay() === 0) {
    date.setDate(date.getDate() + 1)
  }
  return date.toISOString().slice(0, 10)
}

function formatDisplayDate(isoDate: string): string {
  const [year, month, day] = isoDate.split("-").map(Number)
  return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  })
}
