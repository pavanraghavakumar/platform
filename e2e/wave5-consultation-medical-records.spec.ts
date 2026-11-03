import { test, expect } from "@playwright/test"

const doctorEmail = process.env.E2E_DOCTOR_EMAIL ?? "dr.arjun@hms.local"
const doctorPassword = process.env.E2E_DOCTOR_PASSWORD ?? "Doctor@123"

async function signInAsDoctor(page: import("@playwright/test").Page) {
  await page.goto("/Account/Login")
  await page.getByLabel("User name").fill(doctorEmail)
  await page.getByLabel("Password").fill(doctorPassword)
  await page.getByRole("button", { name: "Sign in" }).click()
  await expect(page).toHaveURL(/\/Dashboard\/Doctor$/)
}

test("doctor reviews a checked-in consultation and sees diagnosis validation", async ({ page }) => {
  await signInAsDoctor(page)
  await page.goto("/Consultation/Index?appointmentId=9")

  await expect(page.getByRole("heading", { name: "Consultation — Rahul Bose" })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Patient details" })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Allergies" })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Medical history" })).toBeVisible()
  await expect(page.getByRole("heading", { name: "Last 5 consultations" })).toBeVisible()

  await page.getByRole("button", { name: /Save draft/ }).click()
  await expect(page.getByText("Diagnosis is required.", { exact: true })).toBeVisible()
  await expect(page.getByLabel("Diagnosis")).toBeVisible()
})

test("doctor lists and opens a patient's medical record", async ({ page }) => {
  await signInAsDoctor(page)
  await page.goto("/MedicalRecord?patientId=1")

  await expect(page.getByRole("heading", { name: /Medical records/ })).toBeVisible()
  await expect(page.getByRole("columnheader", { name: "Date" })).toBeVisible()
  await expect(page.getByRole("columnheader", { name: "Doctor" })).toBeVisible()
  await expect(page.getByRole("columnheader", { name: "Diagnosis" })).toBeVisible()
  await expect(page.getByRole("columnheader", { name: "Status" })).toBeVisible()

  await page.getByRole("link", { name: "Open" }).first().click()
  await expect(page.getByRole("heading", { name: /Medical record/ })).toBeVisible()
  await expect(page.getByText("Viral infection", { exact: true })).toBeVisible()
})
