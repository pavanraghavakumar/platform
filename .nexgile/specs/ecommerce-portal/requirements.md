# Requirements: Ecommerce Portal

## Requirement 1: Browse the public catalogue

**User Story:** As a visitor, I want to see the shop's products, so that I can decide whether to apply for a business account.

_Source: BR1, E2, F5, F9_

### Acceptance Criteria

1. WHEN a visitor opens `/` THE SYSTEM SHALL show the public catalogue with product names, photos and descriptions and no prices.
2. WHEN a visitor chooses a product in the public catalogue THE SYSTEM SHALL show its name, description, photos and downloadable documents, and no price.
3. WHEN a visitor clicks a document on a public product page THE SYSTEM SHALL download that document.

## Requirement 2: Sign in and apply for an account

**User Story:** As a business customer, I want to apply for an account and sign in, so that I can buy from the shop at business prices.

_Source: W2, R1, E1, F4, F10_

### Acceptance Criteria

1. WHEN a visitor submits the application form with company name, contact name, email, delivery address, password and a ticked consent box THE SYSTEM SHALL create a Customer account with the status applied and show the message "Your application has been received. We will email you when it is reviewed."
2. IF the application form is submitted with an email that already belongs to a Customer account THEN THE SYSTEM SHALL keep the form open, keep the values the visitor typed and show the message "An account with this email already exists."
3. IF the application form is submitted with the consent box not ticked THEN THE SYSTEM SHALL keep the form open and show the message "Please agree to the privacy notice to apply."
4. WHEN a Customer account with the status approved signs in with its email and password THE SYSTEM SHALL open the catalogue with prices.
5. IF a Customer account with the status applied, rejected or suspended signs in THEN THE SYSTEM SHALL show the message "Your account is not approved for buying yet." and no prices.
6. WHEN a Shop administrator signs in with their email and password THE SYSTEM SHALL ask for a six-digit code sent to their email, and open the Shop administrator dashboard only after the correct code is entered.
7. IF a wrong six-digit code is entered THEN THE SYSTEM SHALL keep the Shop administrator signed out and show the message "That code is not correct."
8. WHEN a Warehouse picker signs in with their email and password THE SYSTEM SHALL open the picking queue.
9. WHEN a signed-in person clicks Sign out THE SYSTEM SHALL end the session and open `/`.

## Requirement 3: Browse the catalogue

**User Story:** As an approved business customer, I want to browse products with live stock, so that I know what I can order before I add it to the basket.

_Source: W1, BR1, E2, F5, F9, N1, N2_

### Acceptance Criteria

1. WHEN an approved business customer opens the catalogue THE SYSTEM SHALL list the active products with name, photo, price in pounds and the stock level read from the shop system.
2. WHEN an approved business customer types a word into the search field THE SYSTEM SHALL list only the products whose name or description contains that word.
3. WHEN an approved business customer chooses a product THE SYSTEM SHALL show its name, description, photos, price, stock level and its downloadable documents.
4. WHEN an approved business customer clicks a document on a product page THE SYSTEM SHALL download that document.
5. WHEN the shop system does not answer THE SYSTEM SHALL show the last known stock level with the label "Stock last checked at <time>" and keep the catalogue usable.
6. WHEN an approved business customer opens a catalogue page THE SYSTEM SHALL show it within 2 seconds.
7. WHILE 40 approved business customers browse the catalogue at the same moment THE SYSTEM SHALL show each of them a catalogue page within 2 seconds.
8. WHILE the portal holds 800 Customer accounts, 2,000 products and 7,000 orders THE SYSTEM SHALL show a catalogue page within 2 seconds.

## Requirement 4: Basket and place an order

**User Story:** As an approved business customer, I want to build a basket and pay by card, so that my order reaches the shop and I get a confirmation.

_Source: W1, BR2, BR3, E3, E4, F3, F16, N2_

### Acceptance Criteria

1. WHEN an approved business customer clicks Add to basket on a product THE SYSTEM SHALL add one of that product to the basket and show the basket count.
2. WHEN an approved business customer opens the basket THE SYSTEM SHALL list each line with product name, quantity, unit price and line total, and the order total in pounds.
3. WHEN an approved business customer changes a line quantity THE SYSTEM SHALL update the line total and the order total.
4. IF a line quantity is more than the stock level the shop system shows THEN THE SYSTEM SHALL keep the quantity at the stock level and show the message "Only <stock level> in stock."
5. WHEN an approved business customer clicks Checkout THE SYSTEM SHALL show the delivery address form filled with the account's delivery address and the order total.
6. WHEN an approved business customer submits the delivery address and pays by card THE SYSTEM SHALL create the order in the shop system with the status paid and show a confirmation with the order number.
7. IF the card payment fails THEN THE SYSTEM SHALL create no order, keep the basket as it was and show the message "Your card was not charged. Please try again."
8. IF the stock level of a basket line has fallen below its quantity when the customer pays THEN THE SYSTEM SHALL create no order and show the message "Stock for <product name> is now <stock level>. Please change your basket."
9. WHEN the shop system does not answer at checkout THE SYSTEM SHALL hold the order, show the message "Your order is held until the shop system is back." and create it in the shop system once the shop system answers.
10. WHEN an approved business customer pays THE SYSTEM SHALL show the confirmation within 5 seconds.

## Requirement 5: See own orders

**User Story:** As an approved business customer, I want to see my orders and their status, so that I know when my order has shipped.

_Source: W3, E3, F12_

### Acceptance Criteria

1. WHEN an approved business customer opens My orders THE SYSTEM SHALL list their orders newest first with order number, date, total and status.
2. WHEN an approved business customer chooses an order THE SYSTEM SHALL show its lines, delivery address, payment status, fulfilment status and tracking number when it is shipped.
3. IF a business customer opens the address of another customer's order THEN THE SYSTEM SHALL show the message "Order not found." and no order details.
4. WHEN a Warehouse picker marks an order shipped with a tracking number THE SYSTEM SHALL show that tracking number on the customer's order page.

## Requirement 6: Approve a business customer

**User Story:** As a Shop administrator, I want to review applications and approve or reject them, so that only approved businesses can buy.

_Source: W2, R2, E1, F15_

### Acceptance Criteria

1. WHEN a Shop administrator opens the dashboard THE SYSTEM SHALL list the Customer accounts with the status applied, oldest first, with company name, contact name, email and application date.
2. WHEN a Shop administrator clicks Approve on an application THE SYSTEM SHALL set the Customer account to approved and send the customer an email that says the account is approved.
3. WHEN a Shop administrator clicks Reject on an application THE SYSTEM SHALL set the Customer account to rejected and send the customer an email that says the account is rejected.
4. WHEN a Shop administrator opens a Customer account THE SYSTEM SHALL show its company name, contact name, email, delivery addresses, approval status and orders.
5. WHEN a Shop administrator clicks Suspend on an approved Customer account THE SYSTEM SHALL set it to suspended and stop the customer seeing prices and placing orders.
6. WHEN a Shop administrator clicks Approve on a suspended Customer account THE SYSTEM SHALL set it to approved again.

## Requirement 7: Manage products

**User Story:** As a Shop administrator, I want to manage products, photos and documents, so that customers see the right products and prices.

_Source: E2, R2, F5_

### Acceptance Criteria

1. WHEN a Shop administrator opens Products THE SYSTEM SHALL list every product with name, price, stock level and status.
2. WHEN a Shop administrator saves a new product with name, description, price and photos THE SYSTEM SHALL add it to the catalogue with the status active.
3. WHEN a Shop administrator changes a product's price THE SYSTEM SHALL show the new price on the catalogue and product page.
4. WHEN a Shop administrator uploads a photo or a document to a product THE SYSTEM SHALL show the photo on the product page and list the document for download.
5. WHEN a Shop administrator sets a product to discontinued THE SYSTEM SHALL remove it from the catalogue and keep it in existing orders.

## Requirement 8: Pick and pack an order

**User Story:** As a Warehouse picker, I want to pick, pack and ship paid orders, so that customers get their goods and can see they are on the way.

_Source: W3, R3, E3, F12_

### Acceptance Criteria

1. WHEN a Warehouse picker opens the picking queue THE SYSTEM SHALL list the paid orders oldest first with order number, customer company name and lines with product name and quantity.
2. WHEN a Warehouse picker clicks Mark picked on an order THE SYSTEM SHALL set it to picked and record the picker's name and the time.
3. WHEN a Warehouse picker clicks Mark packed on a picked order THE SYSTEM SHALL set it to packed and record the picker's name and the time.
4. WHEN a Warehouse picker enters a tracking number and clicks Mark shipped on a packed order THE SYSTEM SHALL set it to shipped with that tracking number and record the picker's name and the time.
5. IF a Warehouse picker clicks Mark packed on an order that is not picked THEN THE SYSTEM SHALL keep the order as it is and show the message "Pick the order before packing it."
6. WHEN a Warehouse picker clicks Report missing item on an order THE SYSTEM SHALL set the order to cancelled, send the Shop administrator an email naming the order and the missing product, and show the customer the status cancelled.

## Requirement 9: Daily orders report

**User Story:** As a Shop administrator, I want a daily orders report, so that I can see the day's orders, their value and their status.

_Source: RP1, R2_

### Acceptance Criteria

1. WHEN a Shop administrator opens Reports THE SYSTEM SHALL show today's orders with order number, customer company name, value in pounds and status, fresh to the minute.
2. WHEN a Shop administrator chooses a date THE SYSTEM SHALL show that day's orders with the same columns.
3. WHEN a Shop administrator clicks Export on the daily report THE SYSTEM SHALL download a CSV file of the orders shown.

## Requirement 10: Shop system connection and alerts

**User Story:** As a Shop administrator, I want the portal to keep working when the shop system is down and to tell me when it breaks, so that no paid order is lost.

_Source: I1, S3, F15, F16, N3_

### Acceptance Criteria

1. WHEN the portal reads stock or writes an order THE SYSTEM SHALL use the shop system as the record of stock and orders.
2. WHEN the shop system does not answer for 60 seconds THE SYSTEM SHALL send the Shop administrator an email that says the shop connection is down.
3. WHEN the shop system answers again after an outage THE SYSTEM SHALL create the held orders in the shop system and send the Shop administrator an email that says the shop connection is back.
4. WHEN a held order is created in the shop system THE SYSTEM SHALL keep its order number and show it on the customer's order page.
5. WHILE the shop system is down THE SYSTEM SHALL keep the portal available and show the last known stock.
6. WHEN a month is measured THE SYSTEM SHALL have answered requests for 99.5% of the month's minutes, with the remaining 0.5% used for maintenance on Sunday nights.
7. IF the portal is down for 60 minutes THEN THE SYSTEM SHALL keep every paid order, so that no paid order is lost.

## Requirement 11: Account records and audit

**User Story:** As the business owner, I want customer records kept as the rules say and every order status change recorded, so that we meet UK GDPR and tax rules.

_Source: E1, E3, E4, F11, F12_

### Acceptance Criteria

1. WHEN a business customer asks for their account to be deleted THE SYSTEM SHALL delete their contact name, email and delivery addresses and keep their orders for 7 years.
2. WHEN an order status changes THE SYSTEM SHALL record the status, the person who changed it and the time, and keep the record for 7 years.
3. WHEN a Shop administrator opens a Customer account THE SYSTEM SHALL show the orders of that customer only.

## Later releases

- RP2 A monthly sales report by customer for the owner, fresh to the day.
- F17 Customer self-service beyond seeing own orders.

## Assumptions

- The existing shop system is reached through an HTTP API with a base URL and an API key set in the environment; the portal reads stock from it and writes paid orders to it.
- Card payment goes through a hosted payment page of a payment provider, so card data never touches the portal; the portal keeps the provider's reference on each Payment.
- Email (application received, approved, rejected, shop connection alerts) is sent through an SMTP server set in the environment.
- A Warehouse picker signs in with an email and password only, without the six-digit code the Shop administrator uses.
- The public catalogue at `/` shows product names, photos and descriptions but no prices, as BR1 says.
- The six-digit code for a Shop administrator is valid for 10 minutes.
- A held order is created in the shop system in the order it was paid, and the customer sees the status held until then.
- The daily orders report shows orders placed on the chosen day in UK time.
- The monthly sales report by customer and customer self-service beyond seeing own orders are later releases.
- A Warehouse picker's picking queue shows paid orders only; a held order joins it once the shop system has it.
