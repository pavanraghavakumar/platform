import { test, expect } from "@playwright/test"

// Issue #15 / 18.2: one demo login per role, each signs in and opens its own dashboard.
const accounts = [
  { email: "admin@hms.local", password: "Admin@123", dashboard: "/Dashboard/Admin", heading: "Admin dashboard" },
  { email: "dr.arjun@hms.local", password: "Doctor@123", dashboard: "/Dashboard/Doctor", heading: "Doctor dashboard" },
  { email: "reception@hms.local", password: "Reception@123", dashboard: "/Dashboard/Receptionist", heading: "Receptionist dashboard" },
  { email: "patient.ravi@hms.local", password: "Patient@123", dashboard: "/Dashboard/Patient", heading: "Patient dashboard" },
]

for (const account of accounts) {
  test(`demo ${account.heading} signs in and opens its own dashboard`, async ({ page }) => {
    await page.goto("/")
    await expect(page).toHaveURL(/\/Account\/Login/)
    await page.getByLabel("User name").fill(account.email)
    await page.getByLabel("Password").fill(account.password)
    await page.getByRole("button", { name: "Sign in" }).click()

    await expect(page).toHaveURL(new RegExp(account.dashboard.replace("/", "\\/")))
    await expect(page.getByRole("heading", { name: account.heading })).toBeVisible()
  })
}
