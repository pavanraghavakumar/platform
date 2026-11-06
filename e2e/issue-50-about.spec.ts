import { expect, test, type Page } from "@playwright/test";

type ViewportExpectation = {
  name: "phone" | "tablet" | "desktop";
  width: number;
  height: number;
  statisticColumns: number;
  heroColumns: number;
};

const viewports: ViewportExpectation[] = [
  { name: "phone", width: 390, height: 844, statisticColumns: 1, heroColumns: 1 },
  { name: "tablet", width: 900, height: 1000, statisticColumns: 2, heroColumns: 2 },
  { name: "desktop", width: 1280, height: 900, statisticColumns: 4, heroColumns: 2 }
];

test("About Hospital renders without clipping at phone, tablet, and desktop widths", async ({ page }) => {
  for (const viewport of viewports) {
    await test.step(viewport.name, async () => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto("/about");

      await expect(page.locator("main#main-content")).toBeVisible();
      await expect(page.locator("article.about-page")).toBeVisible();
      await expect(page.getByRole("heading", { level: 1, name: "Advanced medicine, delivered with humanity" })).toBeVisible();
      await expect(page.getByRole("heading", { name: "A legacy of care, built every day" })).toBeVisible();
      await expect(page.getByRole("heading", { name: "Quality you can feel at every step" })).toBeVisible();
      await expect(page.locator("nav[aria-labelledby='explore-heading']")).toBeVisible();

      for (const href of ["/departments", "/doctors", "/medical-services", "/appointments/book"]) {
        const route = page.locator(`.about-route-grid a[href='${href}']`);
        await expect(route).toBeVisible();
        await expectElementInsideViewport(page, route);
      }

      await expectGridColumns(page.locator(".about-hero__grid"), viewport.heroColumns);
      await expectGridColumns(page.locator(".about-stats"), viewport.statisticColumns);
      await expectGridColumns(page.locator(".about-route-grid"), viewport.statisticColumns);

      const heroCopy = page.locator(".about-hero__copy");
      const heroImage = page.locator(".about-media--hero");
      const copyBox = await heroCopy.boundingBox();
      const imageBox = await heroImage.boundingBox();
      expect(copyBox, `${viewport.name} hero copy must be rendered`).not.toBeNull();
      expect(imageBox, `${viewport.name} hero image must be rendered`).not.toBeNull();

      if (viewport.name === "phone") {
        expect(copyBox!.y + copyBox!.height).toBeLessThanOrEqual(imageBox!.y + 1);
      } else {
        expect(copyBox!.x + copyBox!.width).toBeLessThanOrEqual(imageBox!.x + 1);
      }

      const selectedImage = await page.locator(".about-media--hero img").evaluate((image: HTMLImageElement) => ({
        currentSource: image.currentSrc,
        declaredCandidates: image.srcset,
        sizes: image.sizes,
        renderedWidth: image.getBoundingClientRect().width,
        containerWidth: image.parentElement?.getBoundingClientRect().width ?? 0
      }));
      expect(selectedImage.declaredCandidates).toContain("720w");
      expect(selectedImage.declaredCandidates).toContain("1200w");
      expect(selectedImage.declaredCandidates).toContain("1800w");
      expect(selectedImage.sizes).toBe("(max-width: 767px) 100vw, 50vw");
      expect(selectedImage.currentSource).toMatch(/[?&]w=(720|1200|1800)(?:&|$)/);
      expect(selectedImage.renderedWidth).toBeGreaterThan(0);
      expect(selectedImage.renderedWidth).toBeLessThanOrEqual(selectedImage.containerWidth + 1);

      const hasHorizontalOverflow = await page.evaluate(() =>
        document.documentElement.scrollWidth > document.documentElement.clientWidth + 1);
      expect(hasHorizontalOverflow, `${viewport.name} must not scroll horizontally`).toBe(false);
    });
  }
});

async function expectGridColumns(locator: ReturnType<Page["locator"]>, expected: number) {
  await expect.poll(async () => locator.evaluate(element =>
    getComputedStyle(element).gridTemplateColumns.split(" ").filter(Boolean).length)).toBe(expected);
}

async function expectElementInsideViewport(page: Page, locator: ReturnType<Page["locator"]>) {
  const bounds = await locator.evaluate(element => {
    element.scrollIntoView({ block: "center" });
    const rectangle = element.getBoundingClientRect();
    return { left: rectangle.left, right: rectangle.right };
  });
  const viewport = page.viewportSize();

  expect(bounds.left).toBeGreaterThanOrEqual(-1);
  expect(bounds.right).toBeLessThanOrEqual((viewport?.width ?? 0) + 1);
}
