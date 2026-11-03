import { test, expect } from "@playwright/test"

test("@seed requesting doctor views completed laboratory result and printable report", async ({ page }) => {
  await page.goto("/Account/Login")
  await page.getByRole("textbox", { name: "User name" }).fill(process.env.E2E_DOCTOR_EMAIL ?? "dr.arjun@hms.local")
  await page.getByRole("textbox", { name: "Password" }).fill(process.env.E2E_DOCTOR_PASSWORD ?? "Doctor@123")
  await page.getByRole("button", { name: "Sign in" }).click()
  await expect(page).toHaveURL(/\/Dashboard\/Doctor$/)

  await page.goto("/LabRequest/Details/1")
  await expect(page.getByRole("heading", { name: "Lab request" })).toBeVisible()
  await expect(page.getByText("Ravi Kumar (P-00001)")).toBeVisible()
  await expect(page.getByText("Complete Blood Count")).toBeVisible()
  await expect(page.getByText("4.5-11.0 x10^9/L")).toBeVisible()
  await expect(page.getByText("Within normal range")).toBeVisible()
  await expect(page.getByText("Normal", { exact: true })).toBeVisible()
  await expect(page.getByText("Completed", { exact: true })).toBeVisible()
  await expect(page.getByText(/Result entered/)).toBeVisible()

  await page.goto("/LabRequest/Details/2")
  await expect(page.getByText("You do not have access to this page.")).toBeVisible()

  await page.goto("/LabRequest/Print/1")
  await expect(page.getByRole("heading", { name: "Greenfield General Hospital" })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Laboratory report" })).toBeVisible()
  await expect(page.getByRole("cell", { name: "Ravi Kumar" })).toBeVisible()
  await expect(page.getByRole("cell", { name: "P-00001" })).toBeVisible()
  await expect(page.getByRole("cell", { name: "Complete Blood Count" })).toBeVisible()
  await expect(page.getByRole("cell", { name: "4.5-11.0 x10^9/L" })).toBeVisible()
  await expect(page.getByRole("cell", { name: "Within normal range" })).toBeVisible()
  await expect(page.getByRole("cell", { name: "Normal", exact: true })).toBeVisible()
  await expect(page.getByRole("rowheader", { name: "Date" })).toBeVisible()
  await expect(page.getByRole("button", { name: "Print" })).toBeVisible()
})
