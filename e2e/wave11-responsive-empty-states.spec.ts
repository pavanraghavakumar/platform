import { test, expect } from "@playwright/test"

const adminEmail = process.env.E2E_ADMIN_EMAIL ?? "admin@hms.local"
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? "Admin@123"

test("signed-in layout responds and an empty report renders no table", async ({ page }) => {
  await page.goto("/Account/Login")
  await page.getByLabel("User name").fill(adminEmail)
  await page.getByLabel("Password").fill(adminPassword)
  await page.getByRole("button", { name: "Sign in" }).click()
  await expect(page).toHaveURL(/\/Dashboard\/Admin/)

  const sidebar = page.locator("#hmsSidebar")
  const toggle = page.getByRole("button", { name: "Toggle sidebar" })
  await expect(page.getByRole("searchbox", { name: "Search" })).toBeVisible()
  await expect(page.getByRole("button", { name: "Notifications" })).toBeVisible()
  await expect(page.locator(".hms-profile-btn")).toBeVisible()
  await expect(page.getByRole("link", { name: "Home" })).toBeVisible()
  await expect(page.locator("footer")).toContainText("Greenfield General Hospital")

  if (page.viewportSize()!.width <= 900) {
    await expect(sidebar).toHaveAttribute("aria-hidden", "true")
    await toggle.click()
    await expect(sidebar).toHaveAttribute("aria-hidden", "false")
    await expect(sidebar.getByText("Dashboard", { exact: true })).toBeVisible()
    await page.getByRole("button", { name: "Close menu" }).click()
    await expect(sidebar).toHaveAttribute("aria-hidden", "true")
  } else {
    await expect(sidebar.getByText("Dashboard", { exact: true })).toBeVisible()
    await expect.poll(async () => sidebar.evaluate(el => el.getBoundingClientRect().width)).toBeGreaterThan(200)
    await toggle.click()
    await expect(sidebar.getByText("Dashboard", { exact: true })).toBeHidden()
    await expect.poll(async () => sidebar.evaluate(el => el.getBoundingClientRect().width)).toBeLessThan(100)
    await toggle.click()
    await expect(sidebar.getByText("Dashboard", { exact: true })).toBeVisible()
  }

  await page.goto("/Report/Patients?from=2100-01-01&to=2100-01-02")
  await expect(page.getByRole("heading", { name: "Patient registrations" })).toBeVisible()
  await expect(page.getByText("No records found.", { exact: true })).toBeVisible()
  await expect(page.locator(".report-section table")).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(page.viewportSize()!.width)
})
