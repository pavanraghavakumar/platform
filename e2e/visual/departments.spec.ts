import { test, expect } from "@playwright/test";

test("departments screen keeps its responsive look", async ({ page }) => {
  await page.goto("/departments");
  await expect(page.getByRole("heading", { name: "Hospital departments" })).toBeVisible();
  await expect(page).toHaveScreenshot("departments.png", { animations: "disabled", mask: [page.locator(".department-grid, .department-empty")] });
});

test("department not-found screen keeps its responsive look", async ({ page }) => {
  await page.goto("/departments/unavailable-department");
  await expect(page.getByRole("heading", { name: "This department is not available" })).toBeVisible();
  await expect(page).toHaveScreenshot("department-not-found.png", { animations: "disabled" });
});
