import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-toast',
  templateUrl: './toast.html',
  styleUrl: './toast.scss',
})
export class ToastComponent {
  private readonly toastService = inject(ToastService);

  protected readonly toasts = toSignal(this.toastService.toasts$, {
    initialValue: [],
  });

  dismiss(id: string): void {
    this.toastService.dismiss(id);
  }
}
