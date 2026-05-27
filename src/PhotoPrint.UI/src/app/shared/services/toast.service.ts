import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  message: string;
  type: ToastType;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toasts$$ = new BehaviorSubject<Toast[]>([]);
  readonly toasts$ = this.toasts$$.asObservable();

  show(message: string, type: ToastType = 'info'): void {
    const id = crypto.randomUUID();
    this.toasts$$.next([...this.toasts$$.value, { id, message, type }]);
    setTimeout(() => this.dismiss(id), 5000);
  }

  dismiss(id: string): void {
    this.toasts$$.next(this.toasts$$.value.filter(t => t.id !== id));
  }
}
