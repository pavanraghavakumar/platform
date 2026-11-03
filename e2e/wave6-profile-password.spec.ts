import { test, expect } from "@playwright/test"

const doctorEmail = process.env.E2E_DOCTOR_EMAIL ?? "dr.arjun@hms.local"
const doctorPassword = process.env.E2E_DOCTOR_PASSWORD ?? "Doctor@123"

test("signed-in doctor updates profile and sees short-password validation", async ({ page }) => {
  await page.goto("/Account/Login")
  await page.getByLabel("User name").fill(doctorEmail)
  await page.getByLabel("Password").fill(doctorPassword)
  await page.getByRole("button", { name: "Sign in" }).click()
  await page.goto("/Account/Profile")

  await expect(page.getByRole("heading", { name: "My profile" })).toBeVisible()
  const details = page.getByRole("region", { name: "Account details" })
  await expect(details.getByText(doctorEmail, { exact: true }).first()).toBeVisible()
  await expect(details.getByText("Doctor", { exact: true })).toBeVisible()
  await page.getByRole("textbox", { name: "Phone number" }).fill("9876543210")
  await page.getByRole("button", { name: "Save phone number" }).click()
  await expect(page.getByText("Profile saved.", { exact: true })).toBeVisible()
  await expect(details.getByText("9876543210", { exact: true })).toBeVisible()

  await page.getByRole("link", { name: "Change password" }).click()
  await page.getByLabel("Current password").fill(doctorPassword)
  await page.getByLabel("New password").fill("Short7")
  await page.getByRole("button", { name: "Change password" }).click()
  await expect(page.getByText("Password must be at least 8 characters.", { exact: true })).toBeVisible()
  await expect(page.getByLabel("Current password")).toHaveValue(doctorPassword)
  await expect(page.getByLabel("New password")).toHaveValue("Short7")
})
