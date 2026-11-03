import { test, expect } from "@playwright/test"

const adminEmail = process.env.E2E_ADMIN_EMAIL ?? "admin@hms.local"
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? "Admin@123"

async function signIn(page: import("@playwright/test").Page) {
  await page.goto("/Account/Login")
  await page.getByLabel("User name").fill(adminEmail)
  await page.getByLabel("Password").fill(adminPassword)
  await page.getByRole("button", { name: "Sign in" }).click()
  await expect(page).toHaveURL(/\/Dashboard\/Admin/)
}

test("admin opens and filters the audit trail", async ({ page }) => {
  await signIn(page)
  await page.goto("/Audit")

  await expect(page.getByRole("heading", { name: "Audit trail" })).toBeVisible()
  await expect(page.getByLabel("Record type")).toHaveValue("")
  await expect(page.getByLabel("Start date")).toBeVisible()
  await expect(page.getByLabel("End date")).toBeVisible()

  await page.getByLabel("Record type").selectOption("Payment")
  await page.getByLabel("Start date").fill("2100-01-01")
  await page.getByLabel("End date").fill("2100-01-02")
  await page.getByRole("button", { name: "Filter" }).click()

  await expect(page).toHaveURL(/recordType=Payment/)
  await expect(page.getByLabel("Record type")).toHaveValue("Payment")
  await expect(page.getByLabel("Start date")).toHaveValue("2100-01-01")
  await expect(page.getByLabel("End date")).toHaveValue("2100-01-02")
  await expect(page.getByText("No records found.", { exact: true })).toBeVisible()
  await expect(page.locator("table")).toHaveCount(0)
})
