import { test, expect } from "@playwright/test";

const seededPatientPassword = process.env.E2E_PATIENT_PASSWORD ?? "Greenfield-Patient-2026!";

test("patient registers with browser validation and reaches the secured area", async ({ browser, page }, testInfo) => {
  const suffix = `${Date.now()}-${testInfo.project.name.toLowerCase().replace(/[^a-z0-9]/g, "")}`;
  const email = `journey-${suffix}@example.test`;
  const identifier = `J${Date.now().toString().slice(-9)}${testInfo.project.name === "phone" ? "P" : "D"}`;
  const password = "Journey-Patient-2026!";

  await page.goto("/patient/register");
  await expect(page.getByRole("heading", { name: "Patient Registration" })).toBeVisible();
  await expect(page.getByRole("group", { name: "Account information" })).toBeVisible();
  await expect(page.getByRole("group", { name: "Personal information" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Sign in" })).toHaveAttribute("href", "/patient/login");

  await page.getByRole("textbox", { name: "Email" }).fill(email);
  await page.getByRole("textbox", { name: "Patient identifier" }).fill(identifier);
  await page.getByRole("textbox", { name: "Password", exact: true }).fill(password);
  await page.getByRole("textbox", { name: "Confirm password" }).fill("Different-Password-2026!");
  await page.getByRole("textbox", { name: "Full name" }).fill("Journey Patient");
  await page.getByRole("textbox", { name: "Date of birth" }).fill("1990-04-12");
  await page.getByRole("textbox", { name: "Phone" }).fill("+910000000057");

  await page.getByRole("button", { name: "Show password" }).click();
  await expect(page.getByRole("button", { name: "Hide password" })).toBeVisible();
  await page.getByRole("button", { name: "Create patient account" }).click();
  await expect(page.getByRole("alert")).toContainText("Passwords do not match.");
  await expect(page.getByRole("alert").getByRole("link", { name: "Passwords do not match." }))
    .toHaveAttribute("href", "#ConfirmPassword");
  await expect(page.getByRole("textbox", { name: "Email" })).toHaveValue(email);

  await page.getByRole("textbox", { name: "Confirm password" }).fill(password);
  await page.getByRole("button", { name: "Create patient account" }).click();
  await expect(page).toHaveURL(/\/patient$/);
  await expect(page.getByRole("heading", { name: `Welcome, ${identifier}` })).toBeVisible();
  await expect(page.getByText("Your patient account is securely signed in.")).toBeVisible();
  await expect(page.getByRole("link", { name: "Book an Appointment" }).last()).toHaveAttribute("href", "/appointments/book");

  const identifierContext = await browser.newContext();
  const identifierPage = await identifierContext.newPage();
  await identifierPage.goto("/patient/login");
  await identifierPage.getByRole("textbox", { name: "Email address or patient identifier" }).fill(identifier);
  await identifierPage.getByRole("textbox", { name: "Password" }).fill(password);
  await identifierPage.getByRole("button", { name: "Sign in" }).click();
  await expect(identifierPage).toHaveURL(/\/patient$/);
  await expect(identifierPage.getByRole("heading", { name: `Welcome, ${identifier}` })).toBeVisible();
  await identifierContext.close();
});

test("patient login uses a generic failure and accepts email or patient identifier", async ({ browser }) => {
  const invalidContext = await browser.newContext();
  const invalidPage = await invalidContext.newPage();
  await invalidPage.goto("/patient/login");
  await expect(invalidPage.getByRole("heading", { name: "Patient Login" })).toBeVisible();
  await expect(invalidPage.getByRole("link", { name: "Create an account" })).toHaveAttribute("href", "/patient/register");
  await invalidPage.getByRole("textbox", { name: "Email address or patient identifier" }).fill("missing@example.test");
  await invalidPage.getByRole("textbox", { name: "Password" }).fill("Wrong-Password-2026!");
  await invalidPage.getByRole("button", { name: "Show password" }).click();
  await expect(invalidPage.getByRole("button", { name: "Hide password" })).toBeVisible();
  await invalidPage.getByRole("button", { name: "Sign in" }).click();
  await expect(invalidPage.getByRole("alert")).toContainText("Invalid email address, patient identifier, or password.");
  await expect(invalidPage.getByRole("textbox", { name: "Email address or patient identifier" }))
    .toHaveValue("missing@example.test");
  await expect(invalidPage.getByRole("textbox", { name: "Password" })).toHaveValue("");
  await invalidPage.goto("/patient");
  await expect(invalidPage).toHaveURL(/\/patient\/login(?:\?ReturnUrl=%2Fpatient)?$/);
  await invalidContext.close();

  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto("/patient/login");
  await page.getByRole("textbox", { name: "Email address or patient identifier" }).fill("patient@example.test");
  await page.getByRole("textbox", { name: "Password" }).fill(seededPatientPassword);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/patient$/);
  await expect(page.getByText("Your patient account is securely signed in.")).toBeVisible();
  await context.close();
});

test("unsafe continuation falls back to the secured patient area", async ({ page }) => {
  await page.goto("/patient/login?returnToken=https%3A%2F%2Fevil.example%2Fsteal");
  await page.getByRole("textbox", { name: "Email address or patient identifier" }).fill("patient@example.test");
  await page.getByRole("textbox", { name: "Password" }).fill(seededPatientPassword);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/patient$/);
  await expect(page.getByRole("heading", { name: "Welcome, patient@example.test" })).toBeVisible();
});
