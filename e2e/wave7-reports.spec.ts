import { test, expect } from "@playwright/test"

async function signIn(page: any, user: string, password: string) {
  await page.goto("/")
  await page.getByRole("textbox", { name: "User name" }).fill(user)
  await page.getByRole("textbox", { name: "Password" }).fill(password)
  await page.getByRole("button", { name: "Sign in" }).click()
}

test("admin sees every report and doctor is denied", async ({ browser }) => {
  const admin = await (await browser.newContext()).newPage()
  await signIn(admin, process.env.E2E_ADMIN_EMAIL ?? "admin@hms.local", process.env.E2E_ADMIN_PASSWORD ?? "Admin@123")
  await admin.goto("/Report")
  await expect(admin.getByRole("form", { name: "Report date range" })).toBeVisible()
  for (const heading of ["Patient registrations", "Appointments", "Department activity", "Doctor performance", "Laboratory", "Pharmacy stock", "Revenue"]) {
    await expect(admin.getByRole("heading", { name: heading })).toBeVisible()
  }
  await expect(admin.locator("#revenueChart")).toBeVisible()
  const doctor = await (await browser.newContext()).newPage()
  await signIn(doctor, process.env.E2E_DOCTOR_EMAIL ?? "dr.arjun@hms.local", process.env.E2E_DOCTOR_PASSWORD ?? "Doctor@123")
  await doctor.goto("/Report")
  await expect(doctor.getByText("You do not have access to this page.")).toBeVisible()
})
