import { test, expect } from "@playwright/test";

test("medical services index keeps its responsive look", async ({ page }) => {
  await page.goto("/medical-services");
  await expect(page.getByRole("heading", { level: 1, name: "Medical Services" })).toBeVisible();
  await expect(page).toHaveScreenshot("medical-services.png", {
    animations: "disabled",
    mask: [page.locator(".services-grid")]
  });
});

test("medical service details keep their responsive look", async ({ page }) => {
  await page.goto("/medical-services/advanced-diagnostics");
  await expect(page.getByRole("heading", { level: 1, name: "Advanced Diagnostics" })).toBeVisible();
  await expect(page).toHaveScreenshot("medical-service-details.png", { animations: "disabled" });
});

test("medical service not-found screen keeps its responsive look", async ({ page }) => {
  await page.goto("/medical-services/unavailable-service");
  await expect(page.getByRole("heading", { level: 1, name: "This service is not available" })).toBeVisible();
  await expect(page).toHaveScreenshot("medical-service-not-found.png", { animations: "disabled" });
});
