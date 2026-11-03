import { test, expect } from "@playwright/test"

test("doctor reviews dashboard and scoped patients", async ({ page }) => {
  await page.goto("/")
  await page.getByRole("textbox", { name: "User name" }).fill(process.env.E2E_DOCTOR_EMAIL ?? "dr.arjun@hms.local")
  await page.getByRole("textbox", { name: "Password" }).fill(process.env.E2E_DOCTOR_PASSWORD ?? "Doctor@123")
  await page.getByRole("button", { name: "Sign in" }).click()
  await expect(page.getByRole("heading", { name: "Doctor dashboard" })).toBeVisible()
  for (const label of ["Today's appointments", "Checked In waiting", "Completed consultations today", "Lab results completed today"]) {
    await expect(page.locator(".hms-kpi-label", { hasText: label })).toBeVisible()
  }
  await expect(page.getByRole("heading", { name: "Next 5 upcoming appointments" })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Recent notifications" })).toBeVisible()
  await page.goto("/Patient/DoctorPatients")
  await expect(page.getByRole("heading", { name: "My patients" })).toBeVisible()
  await expect(page.getByRole("columnheader", { name: "Patient ID" })).toBeVisible()
  await expect(page.getByRole("columnheader", { name: "Last appointment" })).toBeVisible()
  await expect(page.locator("tbody tr")).toHaveCount(3)
})
