import { OrderStatusPipe } from './order-status.pipe';

describe('OrderStatusPipe', () => {
  let pipe: OrderStatusPipe;

  beforeEach(() => {
    pipe = new OrderStatusPipe();
  });

  it('maps Paid to Plătită', () => {
    expect(pipe.transform('Paid')).toBe('Plătită');
  });

  it('maps Printing to În tipărire', () => {
    expect(pipe.transform('Printing')).toBe('În tipărire');
  });

  it('maps Shipped to Expediată', () => {
    expect(pipe.transform('Shipped')).toBe('Expediată');
  });

  it('maps Delivered to Livrată', () => {
    expect(pipe.transform('Delivered')).toBe('Livrată');
  });

  it('maps AwaitingPayment to În așteptare', () => {
    expect(pipe.transform('AwaitingPayment')).toBe('În așteptare');
  });

  it('maps Pending to În așteptare', () => {
    expect(pipe.transform('Pending')).toBe('În așteptare');
  });

  it('maps Cancelled to Anulată', () => {
    expect(pipe.transform('Cancelled')).toBe('Anulată');
  });

  it('returns the raw string for an unknown status', () => {
    expect(pipe.transform('Unknown')).toBe('Unknown');
  });

  it('returns empty string for null', () => {
    expect(pipe.transform(null)).toBe('');
  });

  it('returns empty string for undefined', () => {
    expect(pipe.transform(undefined)).toBe('');
  });

  it('returns empty string for empty string', () => {
    expect(pipe.transform('')).toBe('');
  });
});
