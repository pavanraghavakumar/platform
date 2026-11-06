import { test, expect } from "@playwright/test";

test("patient login visual baseline", async ({ page }) => {
  await page.goto("/patient/login");
  await expect(page.getByRole("heading", { name: "Patient Login" })).toBeVisible();
  await expect(page).toHaveScreenshot("patient-login.png", { animations: "disabled", fullPage: true });
});
