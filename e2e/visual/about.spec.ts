import { expect, test } from "@playwright/test";

test("About Hospital responsive appearance", async ({ page }) => {
  await page.goto("/about");
  await expect(page.getByRole("heading", { level: 1, name: "Advanced medicine, delivered with humanity" })).toBeVisible();
  await expect(page.locator(".about-media--hero img")).toBeVisible();
  await expect(page).toHaveScreenshot("about.png", { animations: "disabled" });
});
