import { test, expect } from "@playwright/test"

// Issues #15 and #44 / 17.1, 17.2, 17.3, 17.9, 17.10: shared layout and sidebar behaviour.
test("signed-in page shows the shared layout and the sidebar collapses and expands", async ({ page }) => {
  await page.goto("/Account/Login")
  await page.getByLabel("User name").fill("admin@hms.local")
  await page.getByLabel("Password").fill("Admin@123")
  await page.getByRole("button", { name: "Sign in" }).click()
  await expect(page).toHaveURL(/\/Dashboard\/Admin/)

  // 17.1: one layout with sidebar, top bar (hospital name, search, bell, profile), breadcrumb and footer.
  const sidebar = page.locator("#hmsSidebar")
  await expect(sidebar).toBeVisible()
  await expect(page.getByRole("button", { name: "Toggle sidebar" })).toBeVisible()
  await expect(page.getByRole("searchbox", { name: "Search" })).toBeVisible()
  await expect(page.getByRole("button", { name: "Notifications" })).toBeVisible()
  await expect(page.locator(".hms-profile-btn")).toBeVisible()
  await expect(page.getByRole("link", { name: "Home" })).toBeVisible()
  await expect(page.locator("footer")).toContainText("Greenfield General Hospital")

  // 17.2: one design system — the navy sidebar and the site stylesheet are applied.
  const sidebarBg = await sidebar.evaluate((el) => getComputedStyle(el).backgroundColor)
  expect(sidebarBg).toBe("rgb(15, 47, 92)")

  // 17.9: at 1280px the sidebar is expanded beside the content.
  await page.setViewportSize({ width: 1280, height: 800 })
  await expect(sidebar.getByText("Dashboard", { exact: true })).toBeVisible()
  const expandedWidth = await sidebar.evaluate((el) => el.getBoundingClientRect().width)
  expect(expandedWidth).toBeGreaterThan(200)

  // 17.3: the toggle collapses the sidebar to icons and expands it again on the next click.
  await page.getByRole("button", { name: "Toggle sidebar" }).click()
  await expect(sidebar.getByText("Dashboard", { exact: true })).toBeHidden()
  await expect
    .poll(async () => sidebar.evaluate((el) => el.getBoundingClientRect().width))
    .toBeLessThan(100)

  await page.getByRole("button", { name: "Toggle sidebar" }).click()
  await expect(sidebar.getByText("Dashboard", { exact: true })).toBeVisible()
  await expect
    .poll(async () => sidebar.evaluate((el) => el.getBoundingClientRect().width))
    .toBeGreaterThan(200)

  // 17.10: at a short browser height the sidebar still fills the viewport, while its long
  // navigation scrolls without changing the main document's vertical scroll position.
  await page.setViewportSize({ width: 1280, height: 320 })
  const sidebarNav = page.locator("#hmsSidebarNav")
  const sidebarBox = await sidebar.boundingBox()
  expect(sidebarBox?.height).toBe(320)
  await expect
    .poll(async () => sidebarNav.evaluate((el) => el.scrollHeight > el.clientHeight))
    .toBe(true)

  const documentScrollBefore = await page.evaluate(() => window.scrollY)
  await sidebarNav.evaluate((el) => el.scrollTo({ top: el.scrollHeight }))
  await expect.poll(async () => sidebarNav.evaluate((el) => el.scrollTop)).toBeGreaterThan(0)
  expect(await page.evaluate(() => window.scrollY)).toBe(documentScrollBefore)
})
