import { Pipe, PipeTransform } from '@angular/core';
import { STATUS_LABELS } from '../models/order-status.constants';

@Pipe({
  name: 'orderStatus',
  standalone: true,
  pure: true,
})
export class OrderStatusPipe implements PipeTransform {
  transform(status: string | null | undefined): string {
    if (status == null || status === '') return '';
    return STATUS_LABELS[status] ?? status;
  }
}
