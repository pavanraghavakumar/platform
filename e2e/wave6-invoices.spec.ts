import { test, expect } from "@playwright/test"

const receptionistEmail = process.env.E2E_RECEPTIONIST_EMAIL ?? "reception@hms.local"
const receptionistPassword = process.env.E2E_RECEPTIONIST_PASSWORD ?? "Reception@123"

test("receptionist reviews invoice, rejects overpayment, and opens printable invoice", async ({ page }) => {
  await page.goto("/Account/Login")
  await page.getByLabel("User name").fill(receptionistEmail)
  await page.getByLabel("Password").fill(receptionistPassword)
  await page.getByRole("button", { name: "Sign in" }).click()
  await page.goto("/Invoice")

  await expect(page.getByRole("heading", { name: "Invoices" })).toBeVisible()
  await expect(page.getByLabel("Status")).toBeVisible()
  await page.getByRole("link", { name: "View" }).first().click()
  await expect(page.getByRole("heading", { name: /Invoice/ })).toBeVisible()
  await expect(page.getByText("Outstanding balance", { exact: true })).toBeVisible()

  const amount = page.getByLabel("Amount")
  if (await amount.isVisible()) {
    await amount.fill("999999")
    await page.getByRole("button", { name: /Record payment/ }).click()
    await expect(page.getByText("Payment cannot be more than the outstanding balance.", { exact: true })).toBeVisible()
  }

  const popupPromise = page.waitForEvent("popup")
  await page.getByRole("link", { name: /Print/ }).click()
  const printable = await popupPromise
  await expect(printable.getByText("Greenfield General Hospital", { exact: true })).toBeVisible()
  await expect(printable.getByText("Patient name", { exact: true })).toBeVisible()
  await expect(printable.getByText("Invoice number", { exact: true })).toBeVisible()
  await expect(printable.getByText("Discount", { exact: true })).toBeVisible()
  await expect(printable.getByText(/^Tax/)).toBeVisible()
  await expect(printable.getByText("Total", { exact: true })).toBeVisible()
  await expect(printable.getByText("Paid amount", { exact: true })).toBeVisible()
  await expect(printable.getByText("Outstanding balance", { exact: true })).toBeVisible()
  await expect(printable.getByRole("button", { name: "Print" })).toBeVisible()
})
