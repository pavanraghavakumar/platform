# Tasks: Ecommerce Portal

- [ ] 1. Project setup
  - [ ] 1.1 Create the Next.js skeleton, styles and test runner
    - Create a Next.js App Router app in TypeScript with Prisma, Vitest, Testing Library and Supertest; add `README.md` with the commands of `## Build and run`, a `.gitignore` that includes `.env`, and one passing test.
    - Write the colours, font, spacing and radius of `## Look and feel` into `app/globals.css` as CSS variables every screen's stylesheet uses.
    - _Requirements: 3.1_
    - _Files: package.json, tsconfig.json, next.config.ts, vitest.config.ts, app/layout.tsx, app/globals.css, README.md, .gitignore, tests/setup.test.ts_
    - _Verify: npm install_
    - _Verify: npm test_
  - [ ] 1.2 Add the database schema, migrations and seed script
    - Write the Prisma schema for Customer, StaffUser, Product, ProductFile, BasketItem, Order, OrderLine, Payment, OrderStatusEvent and ShopConnectionState, with the first migration.
    - Write `prisma/seed.ts` with the three test logins of `## Test logins`, run by `Dev` on an empty database and skipped when `NODE_ENV` is `production`; add `.env.example` and a `docker-compose.yml` with a top-level `name:` and a PostgreSQL service.
    - _Requirements: 2.4, 2.6, 2.8_
    - _Files: prisma/schema.prisma, prisma/migrations/**, prisma/seed.ts, .env.example, docker-compose.yml, tests/db.test.ts_
    - _Verify: npm test -- db_
  - [ ] 1.3 Add the session, health path and shop system client
    - Add the signed HTTP-only session cookie with the person and role, `GET /api/health`, `lib/shop-system.ts` with `readStock`, `createOrder` and `ping`, and the Playwright runner with an `e2e/` folder holding a `.gitkeep` that Vitest leaves out.
    - _Requirements: 10.1_
    - _Files: lib/session.ts, lib/shop-system.ts, app/api/health/route.ts, playwright.config.ts, e2e/.gitkeep, vitest.config.ts, tests/shop-system.test.ts_
    - _Verify: npm test -- shop-system_

- [ ] 2. Public catalogue and product page
  - [ ] 2.1 Build the public catalogue at `/` and the public product page
    - Build `/` as a grid of product cards with photo, name and description, a search field and an "Apply for an account" button in the header, and no prices; build the public product page with photos and downloadable documents, and `GET /api/files/:id` to stream them.
    - Style both screens in the style of `## Look and feel`, with the phone and desktop layouts of `### Public catalogue /` and `### Product page`.
    - _Requirements: 1.1, 1.2, 1.3_
    - _Files: app/page.tsx, app/page.module.css, app/products/[id]/page.tsx, app/products/[id]/page.module.css, app/api/public/catalogue/route.ts, app/api/public/catalogue/[id]/route.ts, app/api/files/[id]/route.ts, tests/public-catalogue.test.ts_
    - _Verify: npm test -- public-catalogue_

- [ ] 3. Sign in and apply
  - [ ] 3.1 Build the sign-in, application and six-digit code screens
    - Build the application form with company name, contact name, email, delivery address, password and consent box, the sign-in form, the six-digit code step for a Shop administrator, and Sign out; send each role to its first screen and show the messages the requirements give.
    - Style the screens as the centred card of `### Sign in and apply`, on phone and desktop.
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9_
    - _Files: app/apply/page.tsx, app/apply/page.module.css, app/signin/page.tsx, app/signin/page.module.css, app/signin/verify/page.tsx, app/api/auth/apply/route.ts, app/api/auth/signin/route.ts, app/api/auth/verify-code/route.ts, app/api/auth/signout/route.ts, lib/email.ts, tests/auth.test.ts_
    - _Verify: npm test -- auth_

- [ ] 4. Catalogue with prices and live stock
  - [ ] 4.1 Build the catalogue and product page for approved business customers
    - Build `/catalogue` with a search field, product rows with photo, name, price in pounds and a stock badge read from the shop system, and the product page with price, stock and documents; fall back to the last known stock with "Stock last checked at <time>" when the shop system does not answer.
    - Style both screens as `### Catalogue /catalogue` and `### Product page` say, with the sticky basket bar on phone.
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_
    - _Files: app/catalogue/page.tsx, app/catalogue/page.module.css, app/catalogue/[id]/page.tsx, app/catalogue/[id]/page.module.css, app/api/catalogue/route.ts, app/api/catalogue/[id]/route.ts, lib/stock-cache.ts, tests/catalogue.test.ts_
    - _Verify: npm test -- catalogue_

- [ ] 5. Basket and checkout
  - [ ] 5.1 Build the basket, checkout and confirmation screens
    - Build the basket with a quantity stepper, line totals and order total, the stock cap with "Only <stock level> in stock.", the checkout with the delivery address filled from the account, the hosted payment page and the confirmation with the order number; hold the order when the shop system is down.
    - Style the screens as `### Basket and checkout` says, on phone and desktop.
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10_
    - _Files: app/basket/page.tsx, app/basket/page.module.css, app/checkout/page.tsx, app/checkout/page.module.css, app/orders/[id]/confirmation/page.tsx, app/api/basket/route.ts, app/api/basket/items/route.ts, app/api/basket/items/[id]/route.ts, app/api/checkout/route.ts, app/api/payments/callback/route.ts, lib/payment.ts, lib/orders.ts, tests/checkout.test.ts_
    - _Verify: npm test -- checkout_

- [ ] 6. My orders
  - [ ] 6.1 Build the My orders list and order page
    - Build `/orders` listing the signed-in customer's orders newest first with number, date, total and status, and the order page with lines, delivery address, payment status, fulfilment status and tracking number; show "Order not found." for another customer's order.
    - Style the screens as `### My orders` says, on phone and desktop.
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
    - _Files: app/orders/page.tsx, app/orders/page.module.css, app/orders/[id]/page.tsx, app/orders/[id]/page.module.css, app/api/orders/route.ts, app/api/orders/[id]/route.ts, tests/orders.test.ts_
    - _Verify: npm test -- orders_

- [ ] 7. Shop administrator dashboard and customer account
  - [ ] 7.1 Build the dashboard and the customer account page
    - Build `/admin` listing applications oldest first with company name, contact name, email and date, with Approve and Reject buttons that send the matching email, and the customer account page with details, delivery addresses, status, Suspend and Approve, and that customer's orders only.
    - Style the screens as `### Shop administrator dashboard and customer account` says, on phone and desktop.
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 11.3_
    - _Files: app/admin/page.tsx, app/admin/page.module.css, app/admin/customers/[id]/page.tsx, app/admin/customers/[id]/page.module.css, app/api/admin/customers/route.ts, app/api/admin/customers/[id]/approve/route.ts, app/api/admin/customers/[id]/reject/route.ts, app/api/admin/customers/[id]/suspend/route.ts, tests/admin-customers.test.ts_
    - _Verify: npm test -- admin-customers_

- [ ] 8. Products
  - [ ] 8.1 Build the Products screen
    - Build `/admin/products` listing every product with name, price, stock level and status, with a form to add a product with photos, to change a price, to upload photos and documents, and to set a product discontinued.
    - Style the screen as `### Products` says, on phone and desktop.
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
    - _Files: app/admin/products/page.tsx, app/admin/products/page.module.css, app/api/admin/products/route.ts, app/api/admin/products/[id]/route.ts, app/api/admin/products/[id]/files/route.ts, lib/uploads.ts, tests/admin-products.test.ts_
    - _Verify: npm test -- admin-products_

- [ ] 9. Picking queue
  - [ ] 9.1 Build the picking queue
    - Build `/picking` listing paid orders oldest first with order number, customer company name and lines, with Mark picked, Mark packed, Mark shipped with a tracking number and Report missing item, each recording the picker and the time; show "Pick the order before packing it." when packing an unpicked order.
    - Style the screen as `### Picking queue` says, on phone and desktop.
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_
    - _Files: app/picking/page.tsx, app/picking/page.module.css, app/api/picking/route.ts, app/api/picking/[id]/picked/route.ts, app/api/picking/[id]/packed/route.ts, app/api/picking/[id]/shipped/route.ts, app/api/picking/[id]/missing/route.ts, lib/audit.ts, tests/picking.test.ts_
    - _Verify: npm test -- picking_

- [ ] 10. Reports
  - [ ] 10.1 Build the daily orders report
    - Build `/admin/reports` showing today's orders with order number, customer company name, value in pounds and status, a date field for another day, and an Export button that downloads the rows as CSV.
    - Style the screen as `### Reports` says, on phone and desktop.
    - _Requirements: 9.1, 9.2, 9.3_
    - _Files: app/admin/reports/page.tsx, app/admin/reports/page.module.css, app/api/admin/reports/daily/route.ts, app/api/admin/reports/daily.csv/route.ts, tests/reports.test.ts_
    - _Verify: npm test -- reports_

- [ ] 11. Shop system connection, held orders and retention
  - [ ] 11.1 Add the background job for held orders, connection alerts and retention
    - Add the job that retries held orders in the order they were paid, checks the shop system every 30 seconds and sends the down and back emails once, and purges records past their retention; record every order status change in `OrderStatusEvent` and delete a customer's contact details on request while keeping the orders.
    - _Requirements: 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 11.1, 11.2_
    - _Files: lib/jobs.ts, lib/retention.ts, instrumentation.ts, app/api/admin/customers/[id]/delete/route.ts, tests/jobs.test.ts_
    - _Verify: npm test -- jobs_

- [ ] 12. Deployment setup
  - _Mode: devops_
  - [ ] 12.1 Write the container and managed cloud deploy files
    - Write a `Dockerfile` and `.dockerignore`, a `docker-compose.prod.yml` whose app image is `ecommerce-portal:${APP_TAG}` with the database, and a start command that runs the migrations and then creates the live app's first Shop administrator from `FIRST_ADMIN_EMAIL` and `FIRST_ADMIN_PASSWORD` when both are set and no account exists, never running the seed script.
    - _Requirements: 2.6_
    - _Files: Dockerfile, .dockerignore, docker-compose.prod.yml, scripts/start.sh, .env.example_
    - _Verify: docker compose -f docker-compose.prod.yml config --quiet --no-interpolate_
    - _Verify: docker build -t ecommerce-portal:verify ._
