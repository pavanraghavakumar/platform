import { test, expect } from "@playwright/test";

test("patient registration visual baseline", async ({ page }) => {
  await page.goto("/patient/register");
  await expect(page.getByRole("heading", { name: "Patient Registration" })).toBeVisible();
  await expect(page).toHaveScreenshot("patient-registration.png", { animations: "disabled", fullPage: true });
});
