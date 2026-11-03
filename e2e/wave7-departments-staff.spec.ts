import { test, expect } from "@playwright/test"

async function login(page: any) {
  await page.goto("/")
  await page.getByRole("textbox", { name: "User name" }).fill(process.env.E2E_ADMIN_EMAIL ?? "admin@hms.local")
  await page.getByRole("textbox", { name: "Password" }).fill(process.env.E2E_ADMIN_PASSWORD ?? "Admin@123")
  await page.getByRole("button", { name: "Sign in" }).click()
}

test("admin reviews departments and staff controls", async ({ page }) => {
  await login(page)
  await page.goto("/Department")
  await expect(page.getByRole("heading", { name: "Departments" })).toBeVisible()
  await expect(page.getByRole("searchbox", { name: "Search departments" })).toBeVisible()
  await expect(page.getByRole("combobox", { name: "Sort by" })).toBeVisible()
  await expect(page.getByRole("combobox", { name: "Status filter" })).toBeVisible()
  await expect(page.getByRole("navigation", { name: "Department pagination" })).toBeVisible()
  const firstRow = page.locator("tbody tr").first()
  await expect(firstRow).toContainText("Active")
  await firstRow.getByRole("button", { name: "Deactivate" }).click()
  await expect(page.getByText("Deactivate this department? Its doctors and appointments are kept.")).toBeVisible()
  await page.getByRole("button", { name: "Cancel" }).click()
  await page.goto("/Staff")
  await expect(page.getByRole("heading", { name: "Staff" })).toBeVisible()
  await expect(page.getByRole("searchbox", { name: "Search staff" })).toBeVisible()
  await expect(page.getByRole("navigation", { name: "Staff pagination" })).toBeVisible()
  await expect(page.locator("tbody tr").first()).toContainText("Active")
})
