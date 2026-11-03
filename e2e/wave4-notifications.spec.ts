import { test, expect } from "@playwright/test"

test("Issue 33 notification bell opens the empty panel and unread endpoint is zero", async ({ page }) => {
  await page.goto("/")
  await page.getByLabel("User name").fill(process.env.E2E_ADMIN_EMAIL ?? "admin@hms.local")
  await page.getByLabel("Password").fill(process.env.E2E_ADMIN_PASSWORD ?? "Admin@123")
  await page.getByRole("button", { name: "Sign in" }).click()

  const response = await page.request.get("/Notification/UnreadCount")
  expect(response.ok()).toBeTruthy()
  expect(await response.json()).toEqual({ count: 0 })
  await page.getByRole("button", { name: "Notifications" }).click()
  await expect(page.getByText("You have no notifications.")).toBeVisible()
  await expect(page.getByRole("button", { name: "Notifications" })).toHaveAttribute("aria-expanded", "true")
})
