# Stripe Dashboard Setup (Test Mode)

Complete this once in the [Stripe Dashboard](https://dashboard.stripe.com/test/dashboard) before running billing flows locally.

## 1. Products and prices (organizer SaaS)

Create these products with recurring prices:

| Product   | Price (example) | Notes                          |
|-----------|-----------------|--------------------------------|
| Free      | $0 / month      | Default plan on invite accept  |
| Starter   | $29 / month     | More publish credits           |
| Pro       | $99 / month     | Premium features               |
| Enterprise| Custom          | Sales-assisted                 |

Copy each **Price ID** (`price_...`) into Aspire parameters or `appsettings.Development.json`:

```json
"Stripe": {
  "FreePriceId": "price_...",
  "StarterPriceId": "price_...",
  "ProPriceId": "price_..."
}
```

Optional: add product metadata `unlimited_publishes=true` on paid tiers that should not meter-limit publishes.

## 2. Billing meter

1. Go to **Billing → Meters → Create meter**
2. Event name: `events_published`
3. Aggregation: **Sum**
4. Copy the **Meter ID** (`mtr_...`) into `Stripe:EventsPublishedMeterId`

## 3. Features (entitlements)

Under **Product catalog → Features**, create:

- `event_analytics`
- `promo_codes`
- `check_in`
- `custom_event_slug`

Attach features to each product in the Dashboard.

## 4. Credit grants (free tier)

Free-tier publish limits are granted by the API on first billing setup (1 credit against the `events_published` meter). Paid tiers can receive larger grants via Stripe subscription benefits or recurring credit grants configured in the Dashboard.

## 5. Coupons (organizers)

Create coupons for organizers, e.g.:

| Code           | Discount | Duration  | Metadata              |
|----------------|----------|-----------|-----------------------|
| ORG-LAUNCH20   | 20% off  | 3 months  | `segment=organizer`   |
| ORG-ANNUAL15   | 15% off  | once      | `segment=organizer`   |

Enable **Promotion codes** for each coupon.

## 6. Coupons (ticket buyers — future)

Create separate consumer coupons for Phase 6 ticket checkout:

| Code            | Discount | Metadata             |
|-----------------|----------|----------------------|
| MOBILE-FIRST10  | 10% off  | `segment=consumer`   |
| EARLYBIRD       | Fixed    | `segment=consumer`   |

## 7. Customer Portal

**Settings → Billing → Customer portal**

- Enable plan switching between SaaS prices
- Payment method updates
- Invoice history
- Default return URL: `http://localhost:5173/settings/billing`

## 8. Webhooks

**Developers → Webhooks → Add endpoint**

- Local (Stripe CLI): `stripe listen --forward-to http://localhost:<api-port>/api/webhooks/stripe`
- Production: `https://<api-host>/api/webhooks/stripe`

Events to subscribe:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.paid`
- `invoice.payment_failed`
- `customer.updated`

**Local dev with Stripe CLI:** use the `whsec_...` secret printed when you run `stripe listen` — it is **not** the same as a Dashboard webhook endpoint secret. Copy that value into `Stripe:WebhookSecret` in `appsettings.Development.json` (or the Aspire parameter below), then restart the API.

```bash
stripe listen --forward-to http://localhost:5349/api/webhooks/stripe
```

Use the **HTTP** apiservice port from the Aspire dashboard (`5349` in the default profile), not the HTTPS port (`7383`).

## 9. Aspire parameters

```bash
aspire config set Parameters:stripe-secret-key "sk_test_..." --secret
aspire config set Parameters:stripe-publishable-key "pk_test_..." --secret
aspire config set Parameters:stripe-webhook-secret "whsec_..." --secret
```

Also set meter ID and price IDs via AppHost environment or `appsettings.Development.json`.

## 10. Troubleshooting webhooks

| Symptom | Cause | Fix |
|---------|-------|-----|
| `[400] POST .../webhooks/stripe` | Webhook signing secret mismatch or Stripe CLI API version mismatch | Copy `whsec_...` from `stripe listen`; restart API after code/config updates |
| Billing page shows "Not subscribed" after checkout | Webhooks returned 400, so DB never updated | Fix webhooks **or** complete checkout again — the welcome page now confirms via `/api/billing/confirm-checkout` |
| `Could not confirm checkout session` | Missing `Stripe:SecretKey` (`sk_test_...`) | Add secret key to `appsettings.Development.json` or Aspire parameters |
| `EOF` on port 7383 | Stripe CLI sends HTTP to an HTTPS port | Use `http://localhost:5349/...` instead |
| Billing not updating after checkout | Webhooks failing (400/500) | Fix secret, confirm `[200]` in `stripe listen` output |

## 11. Test cards

- Success: `4242 4242 4242 4242`
- Decline: `4000 0000 0000 0002`
