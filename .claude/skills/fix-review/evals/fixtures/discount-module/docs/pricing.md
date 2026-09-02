# Pricing — discount module (eval fixture)

Synthetic docs for the fix-review skill evals. Nothing here describes real FotoTipar code.

## Rules

- A coupon carries a fixed amount, never a percentage of the basket.
- The discount is subtracted from the order total as given: a discount larger than the total
  produces a negative total, which the payment call reads as a credit to the customer.
- A cart lookup returns every stored line, and the caller decides which of them to show.
- The discount ceiling is 60 percent of the total, stated once per module that needs it.
