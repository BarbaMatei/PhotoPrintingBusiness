export const STATUS_LABELS: Record<string, string> = {
  AwaitingPayment: 'În așteptare',
  Pending: 'În așteptare',
  Paid: 'Plătită',
  Printing: 'În tipărire',
  Shipped: 'Expediată',
  Delivered: 'Livrată',
  PaymentFailed: 'Plată eșuată',
  Cancelled: 'Anulată',
};

export const STATUS_ORDER: string[] = [
  'AwaitingPayment',
  'Paid',
  'Printing',
  'Shipped',
  'Delivered',
];

export function statusClass(status: string): string {
  const map: Record<string, string> = {
    AwaitingPayment: 'status--pending',
    Pending: 'status--pending',
    Paid: 'status--paid',
    Printing: 'status--printing',
    Shipped: 'status--shipped',
    Delivered: 'status--delivered',
    PaymentFailed: 'status--cancelled',
    Cancelled: 'status--cancelled',
  };
  return map[status] ?? 'status--pending';
}

export function isAtLeast(currentStatus: string, referenceStatus: string): boolean {
  const currentIdx = STATUS_ORDER.indexOf(currentStatus);
  const refIdx = STATUS_ORDER.indexOf(referenceStatus);
  if (currentIdx === -1 || refIdx === -1) return false;
  return currentIdx >= refIdx;
}
