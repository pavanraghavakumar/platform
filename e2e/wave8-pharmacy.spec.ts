import { test, expect, Page } from "@playwright/test"

async function signIn(page: Page) {
  await page.goto("/")
  await page.getByRole("textbox", { name: "User name" }).fill(process.env.E2E_ADMIN_EMAIL ?? "admin@hms.local")
  await page.getByRole("textbox", { name: "Password" }).fill(process.env.E2E_ADMIN_PASSWORD ?? "Admin@123")
  await page.getByRole("button", { name: "Sign in" }).click()
}

test("admin manages medicine stock and inventory report", async ({ page }) => {
  await signIn(page)
  const marker = Date.now()
  const category = `Wave category ${marker}`
  const supplier = `Wave supplier ${marker}`
  const medicine = `Wave medicine ${marker}`

  await page.goto("/Medicine/Category")
  await page.getByLabel("Name").fill(category)
  await page.getByRole("button", { name: "Add category" }).click()
  await expect(page.getByRole("cell", { name: category })).toBeVisible()
  await page.goto("/Medicine/Supplier")
  await page.getByLabel("Name").fill(supplier)
  await page.getByRole("button", { name: "Add supplier" }).click()
  await expect(page.getByRole("cell", { name: supplier })).toBeVisible()

  await page.goto("/Medicine/Create")
  await page.getByLabel("Name").fill(medicine)
  await page.getByLabel("Category").selectOption({ label: category })
  await page.getByLabel("Supplier").selectOption({ label: supplier })
  await page.getByLabel("Unit price").fill("42.50")
  await page.getByLabel("Reorder level").fill("5")
  await page.getByLabel("Stock quantity").fill("10")
  await page.getByRole("button", { name: "Save" }).click()
  let row = page.getByRole("row").filter({ hasText: medicine })
  await expect(row).toContainText("In Stock")
  await expect(row).toContainText("10")

  await row.getByRole("link", { name: "Stock in" }).click()
  await page.getByLabel("Batch number").fill(`B-${marker}`)
  await page.getByLabel("Quantity").fill("5")
  await page.getByLabel("Expiry date").fill("2030-12-31")
  await page.getByRole("button", { name: "Save" }).click()
  row = page.getByRole("row").filter({ hasText: medicine })
  await expect(row).toContainText("15")

  await row.getByRole("link", { name: "Stock out" }).click()
  await page.getByLabel("Quantity").fill("16")
  await page.getByLabel("Reason").fill("Wave validation")
  await page.getByRole("button", { name: "Save" }).click()
  await expect(page.getByText("Not enough stock available.")).toBeVisible()
  await page.goto("/Report/Pharmacy")
  await expect(page.getByRole("heading", { name: "Pharmacy stock" })).toBeVisible()
  await expect(page.getByRole("columnheader", { name: "Earliest expiry" })).toBeVisible()
  await expect(page.getByText("Low Stock", { exact: true })).toBeVisible()
  await expect(page.getByText("Out of Stock", { exact: true })).toBeVisible()
})
