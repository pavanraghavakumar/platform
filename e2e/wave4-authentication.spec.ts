import { test, expect, type Page } from "@playwright/test"

type Account = {
  role: string
  email: string
  password: string
  dashboard: string
  heading: string
}

const accounts: Account[] = [
  {
    role: "Admin",
    email: process.env.E2E_ADMIN_EMAIL ?? "admin@hms.local",
    password: process.env.E2E_ADMIN_PASSWORD ?? "Admin@123",
    dashboard: "/Dashboard/Admin",
    heading: "Admin dashboard",
  },
  {
    role: "Doctor",
    email: process.env.E2E_DOCTOR_EMAIL ?? "dr.arjun@hms.local",
    password: process.env.E2E_DOCTOR_PASSWORD ?? "Doctor@123",
    dashboard: "/Dashboard/Doctor",
    heading: "Doctor dashboard",
  },
  {
    role: "Receptionist",
    email: process.env.E2E_RECEPTIONIST_EMAIL ?? "reception@hms.local",
    password: process.env.E2E_RECEPTIONIST_PASSWORD ?? "Reception@123",
    dashboard: "/Dashboard/Receptionist",
    heading: "Receptionist dashboard",
  },
  {
    role: "Patient",
    email: process.env.E2E_PATIENT_EMAIL ?? "patient.ravi@hms.local",
    password: process.env.E2E_PATIENT_PASSWORD ?? "Patient@123",
    dashboard: "/Dashboard/Patient",
    heading: "Patient dashboard",
  },
]

async function signIn(page: Page, account: Account) {
  await page.goto("/Account/Login")
  await page.getByLabel("User name").fill(account.email)
  await page.getByLabel("Password").fill(account.password)
  await page.getByRole("button", { name: "Sign in" }).click()
}

for (const account of accounts) {
  test(`${account.role} signs in to the dashboard for that role`, async ({ page }) => {
    await signIn(page, account)

    await expect(page).toHaveURL(new RegExp(`${account.dashboard.replaceAll("/", "\\/")}$`))
    await expect(page.getByRole("heading", { name: account.heading })).toBeVisible()
  })
}

test("a signed-in person stays signed in after access denial and can sign out", async ({ page }) => {
  const doctor = accounts.find(account => account.role === "Doctor")!
  await signIn(page, doctor)
  await expect(page).toHaveURL(/\/Dashboard\/Doctor$/)

  await page.goto("/Dashboard/Admin")
  await expect(page).toHaveURL(/\/Account\/AccessDenied/)
  await expect(page.getByText("You do not have access to this page.", { exact: true })).toBeVisible()

  await page.getByRole("link", { name: "Back to dashboard" }).click()
  await expect(page).toHaveURL(/\/Dashboard\/Doctor$/)

  await page.locator("#hmsProfileBtn").click()
  await page.getByRole("button", { name: "Sign out" }).click()

  await expect(page).toHaveURL(/\/Account\/Login$/)
  await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible()
})
