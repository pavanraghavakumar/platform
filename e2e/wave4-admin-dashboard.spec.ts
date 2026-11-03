import { test, expect } from "@playwright/test"

test("Issue 18 Admin sees database-backed dashboard", async ({ page }) => {
  await page.goto("/")
  await page.getByLabel("User name").fill(process.env.E2E_ADMIN_EMAIL ?? "admin@hms.local")
  await page.getByLabel("Password").fill(process.env.E2E_ADMIN_PASSWORD ?? "Admin@123")
  await page.getByRole("button", { name: "Sign in" }).click()
  await expect(page).toHaveURL(/\/Dashboard\/Admin/)

  for (const label of ["Total patients", "Total doctors", "Total staff", "Today's appointments", "Pending appointments", "Completed consultations", "Total revenue", "Outstanding balance"]) {
    await expect(page.getByText(label, { exact: true })).toBeVisible()
  }
  await expect(page.locator(".hms-kpi-label").getByText("Departments", { exact: true })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Revenue — last 6 months" })).toBeVisible()
  await expect(page.getByLabel("Revenue of the last 6 months")).toBeVisible()
  await expect(page.getByRole("heading", { name: "Appointments — last 7 days" })).toBeVisible()
  await expect(page.getByLabel("Appointment trend of the last 7 days")).toBeVisible()
  await expect(page.getByRole("heading", { name: "Recent patient registrations" })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Recent appointments" })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Departments", exact: true })).toBeVisible()
  await expect(page.getByRole("table").nth(0).locator("tbody tr")).toHaveCount(5)
  await expect(page.getByRole("table").nth(1).locator("tbody tr")).toHaveCount(5)
})
