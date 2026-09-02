export const MAX_DISCOUNT_PERCENT = 60

export function applyDiscount(total, discount) {
  return total - discount
}

export function describeDiscount(total, discount) {
  return `${discount} off ${total}`
}
