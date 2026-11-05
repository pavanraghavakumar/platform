import { test, expect } from "@playwright/test";

test("guest browses departments and recovers from an unavailable department", async ({ page }) => {
  await page.goto("/departments");
  await expect(page.getByRole("heading", { name: "Hospital departments" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Care for every stage of life" })).toBeVisible();

  const departmentLinks = page.locator(".department-card h3 a");
  if (await departmentLinks.count()) {
    const departmentName = (await departmentLinks.first().textContent())!.trim();
    await departmentLinks.first().click();
    await expect(page.getByRole("heading", { level: 1, name: departmentName })).toBeVisible();
    await expect(page.locator(".department-identity__media img")).toBeVisible();
    await expect(page.getByRole("link", { name: "Book an Appointment" }).first()).toHaveAttribute("href", /\/appointments\/book\?department=/);
    await expect(page.getByRole("link", { name: "View our doctors" })).toHaveAttribute("href", /\/doctors\?department=/);
  } else {
    await expect(page.getByRole("status")).toContainText("Department information is being updated");
  }

  const response = await page.goto("/departments/unavailable-department");
  expect(response?.status()).toBe(404);
  await expect(page.getByRole("heading", { name: "This department is not available" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Back to Departments" })).toHaveAttribute("href", "/departments");
});
