import { test, expect } from "@playwright/test";

const forbiddenOperation = /book|checkout|purchase|payment|card|charge|tax|refund|invoice/i;

async function expectInformationOnly(page: import("@playwright/test").Page) {
  await expect(page.locator("form")).toHaveCount(0);
  await expect(page.getByRole("button")).toHaveCount(0);

  const destinations = await page.locator("a[href], form[action]").evaluateAll(elements =>
    elements.map(element => element.getAttribute("href") ?? element.getAttribute("action") ?? ""));
  expect(destinations.filter(destination => forbiddenOperation.test(destination))).toEqual([]);
}

test("guest reads published health packages and recovers from an unavailable package", async ({ page }) => {
  await page.goto("/health-packages");
  await expect(page.getByRole("heading", { level: 1, name: "Health Packages" })).toBeVisible();
  await expect(page.getByRole("note")).toContainText("information only");

  const cards = page.locator(".health-package-card");
  await expect(cards).toHaveCount(3);
  await expect(cards).toContainText([
    "Essential Health Check",
    "Heart Wellness Profile",
    "Women's Wellness Check"
  ]);
  const essentialCard = cards.filter({ hasText: "Essential Health Check" });
  for (const text of [
    "A practical preventive screening package designed to establish a clear picture of your everyday health.",
    "Complete blood count and blood sugar screening",
    "Kidney and liver function profile",
    "Cholesterol and thyroid screening",
    "General physician review of your results"
  ]) {
    await expect(essentialCard).toContainText(text);
  }
  await expectInformationOnly(page);

  await page.getByRole("link", { name: "Essential Health Check", exact: true }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Essential Health Check" })).toBeVisible();
  await expect(page.getByText("A practical preventive screening package designed to establish a clear picture of your everyday health.")).toBeVisible();
  const detailList = page.locator(".health-package-detail__list");
  for (const text of [
    "Complete blood count and blood sugar screening",
    "Kidney and liver function profile",
    "Cholesterol and thyroid screening",
    "General physician review of your results"
  ]) {
    await expect(detailList).toContainText(text);
  }
  await expect(page.getByRole("heading", { level: 2, name: "Before your health check" })).toBeVisible();
  await expectInformationOnly(page);

  const response = await page.goto("/health-packages/not-a-current-package");
  expect(response?.status()).toBe(404);
  await expect(page.getByText("Meridian Hospital", { exact: true }).first()).toBeVisible();
  await expect(page.getByRole("heading", { level: 1, name: "We couldn't find that health package" })).toBeVisible();
  await expect(page.getByText("The package may be unpublished, withdrawn, archived, or no longer available.")).toBeVisible();
  const returnLink = page.getByRole("link", { name: "Return to Health Packages" });
  await expect(returnLink).toHaveAttribute("href", "/health-packages");
  await returnLink.click();
  await expect(page.getByRole("heading", { level: 1, name: "Health Packages" })).toBeVisible();
});
