import { test, expect } from "@playwright/test";

test("guest browses published medical services, opens details, and recovers from unavailable service", async ({ page }) => {
  await page.goto("/medical-services");
  await expect(page.getByRole("heading", { level: 1, name: "Medical Services" })).toBeVisible();

  const cards = page.locator(".service-card");
  await expect(cards).toHaveCount(3);
  await expect(cards).toContainText([
    "Advanced Diagnostics",
    "Preventive Care",
    "Rehabilitation"
  ]);
  await expect(cards.locator("img")).toHaveCount(3);

  await cards.filter({ hasText: "Advanced Diagnostics" }).getByRole("link", { name: "Explore this service" }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Advanced Diagnostics" })).toBeVisible();
  await expect(page.getByText("Clear answers through modern imaging and laboratory expertise.")).toBeVisible();
  await expect(page.getByText("Our diagnostic teams bring imaging, pathology, and specialist interpretation together to help patients and clinicians make informed care decisions.")).toBeVisible();
  await expect(page.getByText("Your care team will explain preparation, what to expect during the test, and how results are shared.")).toBeVisible();
  await expect(page.locator(".service-hero img")).toBeVisible();
  await expect(page.locator(".service-hero").getByRole("link", { name: "Book an Appointment" })).toHaveAttribute("href", "/appointments/book?service=advanced-diagnostics");

  const response = await page.goto("/medical-services/unavailable-service");
  expect(response?.status()).toBe(404);
  await expect(page.getByRole("heading", { level: 1, name: "This service is not available" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Back to Medical Services" })).toHaveAttribute("href", "/medical-services");
});
