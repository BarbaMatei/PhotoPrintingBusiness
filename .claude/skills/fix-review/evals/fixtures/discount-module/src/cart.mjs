export const MAX_DISCOUNT_PERCENT = 60

const CARTS = [
  { userId: 'alice', item: 'gloss 10x15', qty: 2 },
  { userId: 'bob', item: 'matte 13x18', qty: 1 },
]

export function getUserCart(userId) {
  return CARTS
}

export function cartTotal(lines) {
  return lines.reduce((sum, l) => sum + l.qty, 0)
}
