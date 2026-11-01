# Requirements brief — Ecommerce Portal
## Profile
An online portal for an e-commerce website, letting customers browse and buy products. · web app · e-commerce · size: business · depth: standard · money, personalData, integrations, files
- **Has a user interface?** Yes (A portal is a set of screens customers use in a browser.)
- **Runs locally?** Yes (It is a web portal that serves pages to a browser, so Greenfield can start it on the person's machine with a local database and a stand-in for the shop system.)
## The app
An online portal for an existing e-commerce website where approved business customers sign in to browse products, see live stock and pay by card, with shop staff approving customers and warehouse staff picking orders. It connects to the existing shop system for stock and orders, and shows product photos and downloadable documents.
## Goals and success
- G1 Give the e-commerce website a portal where customers can browse and buy. (description)
- M1 At least 200 orders a month from approved business customers within six months of launch, with at least 60% of those customers ordering again. [assumed] (Q6)
## Roles
- R1 Approved business customers who sign in to browse and buy. — Approved business customer (Q1, Q3)
- R2 Shop staff who approve business customers, manage products and handle orders. — Shop administrator (2 to 3 people) [assumed] (assumption)
- R3 Warehouse staff who pick and pack the orders the portal receives. — Warehouse picker (3 to 5 people) [assumed] (assumption)
## Permissions
- R1 Approved business customer may place an order, see own orders (assumption)
- R2 Shop administrator may approve a business customer, manage products (assumption)
- R3 Warehouse picker may mark an order picked and packed (assumption)
## Business events
- EV1 A business customer places an order. [assumed] (assumption)
- EV2 A card payment succeeds or fails. [assumed] (assumption)
- EV3 Stock in the shop system falls below the level a customer is ordering. [assumed] (assumption)
- EV4 An order has not been picked 24 hours after it was paid. [assumed] (assumption)
## Workflows
### W1 Place an order (release 1) — actors: R1 Approved business customer; trigger: An approved business customer needs to buy products.; outcome: A paid order exists in the shop system and the customer has a confirmation.; how often: About 200 orders a month at first. [assumed] (assumption)
1. The customer signs in and browses the catalogue.
2. The customer adds products to the basket and sees live stock from the shop system.
3. The customer reviews the basket and confirms the delivery address.
4. The customer pays by card.
5. The portal creates the order in the shop system and shows the customer a confirmation.
- The card payment fails.
- Stock runs out between adding and paying.
- X1 The card payment fails. [assumed] (assumption)
- X2 Stock runs out between adding to the basket and paying. [assumed] (assumption)
### W2 Approve a business customer (release 1) — actors: R2 Shop administrator; trigger: A business applies for an account.; outcome: An approved business customer can sign in and buy.; how often: A few a week. [assumed] (assumption)
1. The business fills in its company details and applies.
2. The shop administrator sees the application in the portal.
3. The administrator checks the company and approves or rejects it.
4. The customer is told by email and can sign in.
- The company details cannot be checked.
- X3 The company details cannot be checked. [assumed] (assumption)
### W3 Pick and pack an order (release 1) — actors: R3 Warehouse picker; trigger: A paid order arrives in the shop system.; outcome: The order is on its way and the customer can see it was shipped.; how often: Daily. [assumed] (assumption)
1. The warehouse picker sees the new order in the portal.
2. The picker picks the items and marks them picked.
3. The picker packs the order and marks it packed.
4. The order is marked shipped with a tracking number.
- An item is missing from the shelf.
- X4 An item is missing from the shelf. [assumed] (assumption)
## Records
- E1 Customer account. fields: company name, contact name, email, delivery addresses, approval status; states: applied → approved → rejected → suspended; owner: R2; how many: About 300 in year 1, 800 in year 3.; kept for: While the account is open and 2 years after. [assumed, high impact] (assumption)
- E2 Product. fields: name, description, price, stock level, photos, documents; states: active → out of stock → discontinued; owner: R2; how many: About 2,000 products.; kept for: While sold and 3 years after. [assumed, high impact] (assumption)
- E3 Order. fields: customer, lines with product and quantity, prices, delivery address, payment status, fulfilment status, tracking number; states: placed → paid → picked → packed → shipped → cancelled → refunded; owner: R1; how many: About 2,400 in year 1, 7,000 in year 3.; kept for: 7 years for tax. [assumed, high impact] (assumption)
- E4 Payment. fields: order, amount, card result, reference from the payment provider; states: authorised → captured → failed → refunded; owner: R2; how many: One per order.; kept for: 7 years. [assumed, high impact] (assumption)
## Rules
- BR1 Only approved business customers can see prices and place orders.. Applies: An approved customer signs in and sees prices.. Does not apply: A visitor who has not applied sees only the public catalogue with no prices. [assumed] (assumption)
- BR2 An order is only created once the card payment has succeeded.. Applies: A successful card payment creates the order.. Does not apply: A failed card payment leaves no order. [assumed] (assumption)
- BR3 A customer cannot order more of a product than the shop system shows in stock.. Applies: Ordering 5 when 10 are in stock is allowed.. Does not apply: Ordering 5 when 2 are in stock is blocked. [assumed] (assumption)
## Quality targets
- N1 [scale] Up to 40 customers browsing at the busiest moment, around 11am on Mondays; about 300 customer accounts and 2,000 products in year 1, 800 accounts and 7,000 orders in year 3.: 40 customers at the busiest moment [assumed] (assumption)
- N2 [performance] A catalogue page loads in under 2 seconds and paying takes under 5 seconds.: 2 seconds for a page [assumed] (assumption)
- N3 [availability] The portal is available 99.5% of the month, with maintenance on Sunday nights; an hour of outage costs about £1,000 in lost orders, and no paid order may be lost.: 99.5% uptime [assumed] (assumption)
## Integrations
- I1 The portal connects to the existing shop system for stock and orders. (Q4)
## Reports
- RP1 A daily orders report for the shop administrator showing new orders, their value and their status, fresh to the minute. [assumed] (assumption)
- RP2 A monthly sales report by customer for the owner, fresh to the day. [assumed] (assumption)
## Scope and constraints
- S1 Browsing the catalogue, placing orders, card payment, customer accounts, product photos and downloadable documents, and connection to the shop system. (description, Q1, Q2, Q3, Q4, Q5)
- S3 The portal must use the existing shop system for stock and orders. [constraint] (Q4)
## Decisions
- **Stack:** Next.js, Node.js and PostgreSQL (decided by Greenfield; suggested: It gives fast catalogue pages and a clean connection to the shop system, with one language across the portal.) (Q7)
- **Deploy target:** A managed cloud host (decided by Greenfield; suggested: It handles busy shopping times without the shop running servers itself.) (Q8)
## Assumptions
- A1 The portal is working when it brings in at least 200 orders a month from approved business customers within six months of launch, with at least 60% of those customers ordering again. — It ties success to real buying behaviour rather than visits, and gives a date to judge it by.; high impact [assumed] (assumption, Q6)
## Other facts
- F1 The portal is for an existing e-commerce website. [scope] (description)
- F2 Only approved business customers can buy; the public cannot. [stakeholders] (Q1)
- F3 Customers pay by card online. [money] (Q2)
- F4 Customers have accounts and sign in. [stakeholders] (Q3)
- F5 The portal shows product photos and documents customers can download. [files] (Q5)
- F6 Today orders from business customers come in by email and phone, and staff type them into the shop system by hand. [current] [assumed] (assumption)
- F7 The top pains today are typing orders twice, wrong prices quoted to customers, and no way for customers to see stock. [current] [assumed] (assumption)
- F8 Today's tools are the shop system, email, phone and a shared spreadsheet of approved customers. [current] [assumed] (assumption)
- F9 The closest products are the trade portals of large wholesalers; copy their clear stock display and simple reordering, avoid their cluttered screens. [similar] [assumed] (assumption)
- F10 Customers sign in with an email and password, and administrators use two steps; the worst misuse is one business seeing another's prices, and card data never touches the portal. [security] [assumed, high impact] (assumption)
- F11 The portal holds company contact names, emails and delivery addresses for UK users under UK GDPR, kept as the records say, with deletion on request and consent at sign-up. [privacy] [assumed, high impact] (assumption)
- F12 Every order status change is recorded with who did it and when, kept for 7 years. [audit] [assumed, high impact] (assumption)
- F13 The portal meets WCAG 2.2 AA, is in English only, left to right, on UK time and in pounds. [access_i18n] [assumed] (assumption)
- F14 The portal is used on desktop and phone in the office and the warehouse, and needs no offline use. [devices] [assumed] (assumption)
- F15 The shop administrator is alerted by email when the portal or the shop connection breaks, and support can see a customer's orders but not act as them. [operations] [assumed] (assumption)
- F16 Stock and orders flow both ways with the shop system: stock is read live, orders are written when paid, and if the shop system is down the portal shows the last known stock and holds orders until it is back. [integrations] [assumed, high impact] (assumption)
- F17 The first release has placing an order, approving a customer and picking and packing; later releases add richer reports and customer self-service. [release] [assumed, high impact] (assumption)
- F18 The likeliest failures are the shop system connection breaking, card payments failing, and customers not being approved quickly enough to buy. [risks] [assumed] (assumption)
## Out of scope and later releases
- S2 Selling to the public, and a separate stock system of its own. [out of scope] (Q1, Q4)
## The interview
- Q1 [stakeholders · owner] Does the portal sell to the public, or only to signed-in business customers? → Only approved business customers
- Q2 [money · owner] How does a customer pay? → Card online
- Q3 [stakeholders · owner] Does the portal keep customer accounts with sign-in? → Yes, customers sign in
- Q4 [integrations · owner] Must the portal connect to the existing shop system for stock and orders? → Yes, it must
- Q5 [files · owner] Should the portal show product photos and documents customers can download? → Yes, photos and files
- Q6 [success · owner] When the portal has been live for a while, what would tell you it is working well? → Decide for me (settled: The portal is working when it brings in at least 200 orders a month from approved business customers within six months of launch, with at least 60% of those customers ordering again.)
## Decided by Greenfield (Enough, decide the rest)
- Q7 Which stack should the portal be built with? → Decide for me (settled: Next.js, Node.js and PostgreSQL)
- Q8 Where should the portal be deployed? → Decide for me (settled: A managed cloud host)
