import { test, expect, Page } from "@playwright/test"

async function signIn(page: Page) {
  await page.goto("/")
  await page.getByRole("textbox", { name: "User name" }).fill(process.env.E2E_ADMIN_EMAIL ?? "admin@hms.local")
  await page.getByRole("textbox", { name: "Password" }).fill(process.env.E2E_ADMIN_PASSWORD ?? "Admin@123")
  await page.getByRole("button", { name: "Sign in" }).click()
}

test("admin manages doctors and validates schedules", async ({ page }) => {
  await signIn(page)
  await page.goto("/Doctor")
  await expect(page.getByRole("columnheader", { name: "Consultation fee" })).toBeVisible()
  await page.getByLabel("Department filter").selectOption({ label: "Cardiology" })
  await page.getByRole("button", { name: "Apply" }).click()
  await expect(page.getByRole("cell", { name: "Cardiology", exact: true }).first()).toBeVisible()

  const marker = Date.now()
  const name = `Dr Wave Eight ${marker}`
  await page.getByRole("link", { name: "New doctor" }).click()
  await page.getByLabel("Full name").fill(name)
  await page.getByLabel("Email").fill(`wave8-doctor-${marker}@hms.local`)
  await page.getByLabel("Phone").fill("9000000001")
  await page.getByLabel("Specialization").fill("Cardiology")
  await page.getByLabel("Department").selectOption({ label: "Cardiology" })
  await page.getByLabel("Qualifications").fill("MD Cardiology")
  await page.getByLabel("Years of experience").fill("8")
  await page.getByLabel("Consultation fee").fill("875")
  await page.getByLabel("Temporary password").fill("Doctor@123")
  await page.getByRole("button", { name: "Save" }).click()
  const row = page.getByRole("row").filter({ hasText: name })
  await expect(row).toContainText("₹875.00")
  await expect(row).toContainText("Active")

  await row.getByRole("link", { name: "Edit" }).click()
  const monday = page.locator(".hms-schedule-row").filter({ hasText: "Monday" })
  await monday.getByRole("checkbox").check()
  await monday.getByLabel("Start time").fill("10:00")
  await monday.getByLabel("End time").fill("09:00")
  await page.getByRole("button", { name: "Save" }).click()
  await expect(page.getByText("End time must be after start time.")).toBeVisible()
  await monday.getByLabel("End time").fill("16:00")
  await page.getByLabel("Consultation fee").fill("925")
  await page.getByRole("button", { name: "Save" }).click()
  await expect(page.getByRole("heading", { name })).toBeVisible()
  await expect(page.getByText("₹925.00")).toBeVisible()
  await expect(page.getByRole("heading", { name: "Working schedule" })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Appointment history" })).toBeVisible()
})
