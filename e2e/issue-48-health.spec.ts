import { expect, test } from "@playwright/test";

test("public health endpoint reports a safe healthy status", async ({ page }) => {
  const response = await page.goto("/health");

  expect(response).not.toBeNull();
  expect(response!.status()).toBe(200);
  expect(response!.headers()["content-type"]).toContain("application/json");
  expect(response!.headers()["content-security-policy"]).toContain("default-src 'self'");
  expect(response!.headers()["x-content-type-options"]).toBe("nosniff");
  expect(response!.headers()["x-frame-options"]).toBe("DENY");
  expect(response!.headers()["x-correlation-id"]).toBeTruthy();
  await expect(page.locator("body")).toHaveText('{"status":"Healthy"}');
});
