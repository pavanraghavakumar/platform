import { test, expect } from "@playwright/test"

test("Issue 22 Receptionist searches the patient list and opens a complete profile", async ({ page }) => {
  await page.goto("/")
  await page.getByLabel("User name").fill(process.env.E2E_RECEPTIONIST_EMAIL ?? "reception@hms.local")
  await page.getByLabel("Password").fill(process.env.E2E_RECEPTIONIST_PASSWORD ?? "Reception@123")
  await page.getByRole("button", { name: "Sign in" }).click()
  await page.goto("/Patient")

  await expect(page.getByRole("searchbox", { name: "Search patients" })).toBeVisible()
  await expect(page.getByRole("combobox", { name: "Sort patients" })).toBeVisible()
  await expect(page.getByRole("combobox", { name: "Filter by status" })).toBeVisible()
  await expect(page.getByRole("navigation", { name: "Pagination" })).toBeVisible()
  await expect(page.locator("tbody tr")).toHaveCount(10)

  await page.getByRole("searchbox", { name: "Search patients" }).fill("P-00003")
  await page.getByRole("button", { name: "Apply" }).click()
  await expect(page.locator("tbody tr")).toHaveCount(1)
  await expect(page.getByText("Sanjay Patel")).toBeVisible()
  await page.getByRole("link", { name: "View" }).click()

  for (const heading of ["Personal and contact details", "Emergency contact", "Medical history", "Allergies", "Appointment history"]) {
    await expect(page.getByRole("heading", { name: heading })).toBeVisible()
  }
  await expect(page.getByText("Dr. Vikram Singh")).toBeVisible()
  await expect(page.getByText("30 Oct 2026")).toBeVisible()
  await expect(page.getByText("11:00")).toBeVisible()
  await expect(page.getByText("Completed", { exact: true })).toBeVisible()
})
