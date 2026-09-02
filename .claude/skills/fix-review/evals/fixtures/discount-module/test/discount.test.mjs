import { test } from 'node:test'
import assert from 'node:assert/strict'
import { applyDiscount, describeDiscount } from '../src/discount.mjs'
import { cartTotal, getUserCart } from '../src/cart.mjs'

test('a discount below the total is subtracted', () => {
  assert.equal(applyDiscount(100, 10), 90)
})

test('the discount description names both figures', () => {
  assert.equal(describeDiscount(100, 10), '10 off 100')
})

test('a cart total sums the line quantities', () => {
  assert.equal(cartTotal([{ qty: 2 }, { qty: 1 }]), 3)
})

test('a known user has a cart', () => {
  assert.ok(getUserCart('alice').length > 0)
})
