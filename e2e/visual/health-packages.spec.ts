import { test, expect } from "@playwright/test";

test("health packages index keeps its responsive look", async ({ page }) => {
  await page.route("**/css/pages/health-packages.css*", async route => {
    const response = await route.fetch();
    const stylesheet = await response.text();
    await route.fulfill({
      response,
      body: `${stylesheet}\n.health-package-grid { height: 720px; overflow: hidden; }`
    });
  });
  await page.goto("/health-packages");
  await expect(page.getByRole("heading", { level: 1, name: "Health Packages" })).toBeVisible();
  await expect(page).toHaveScreenshot("health-packages.png", {
    animations: "disabled",
    mask: [page.locator(".health-package-grid")]
  });
});

test("health package details keep their responsive look", async ({ page }) => {
  await page.goto("/health-packages/essential-health-check");
  await expect(page.getByRole("heading", { level: 1, name: "Essential Health Check" })).toBeVisible();
  await expect(page).toHaveScreenshot("health-package-details.png", { animations: "disabled" });
});

test("health package not-found screen keeps its responsive look", async ({ page }) => {
  const response = await page.goto("/health-packages/not-a-current-package");
  expect(response?.status()).toBe(404);
  await expect(page.getByRole("heading", { level: 1, name: "We couldn't find that health package" })).toBeVisible();
  await expect(page).toHaveScreenshot("health-package-not-found.png", { animations: "disabled" });
});
