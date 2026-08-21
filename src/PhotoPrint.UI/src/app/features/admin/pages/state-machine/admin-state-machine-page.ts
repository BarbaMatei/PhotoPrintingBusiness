import { ChangeDetectionStrategy, Component } from '@angular/core';

interface StateNode {
  id: string;
  label: string;
  description: string;
  colorClass: string;
  isTerminal?: boolean;
}

interface Transition {
  from: string;
  to: string;
  trigger: string;
  actor: 'system' | 'admin';
}

@Component({
  selector: 'app-admin-state-machine-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './admin-state-machine-page.html',
  styleUrl: './admin-state-machine-page.scss',
})
export class AdminStateMachinePage {
  readonly states: StateNode[] = [
    {
      id: 'AwaitingPayment',
      label: 'În așteptarea plății',
      colorClass: 'state--awaiting',
      description: 'Comanda a fost creată dar plata nu a fost confirmată. Coșul este rezervat 30 min.',
    },
    {
      id: 'Paid',
      label: 'Plătit',
      colorClass: 'state--paid',
      description: 'Plata a fost confirmată de Stripe / EuPlatesc. Comanda intră în coada de producție.',
    },
    {
      id: 'Printing',
      label: 'În curs de printare',
      colorClass: 'state--printing',
      description: 'Fotografiile sunt trimise la imprimantă. Adminul mută manual comanda în acest status.',
    },
    {
      id: 'Shipped',
      label: 'Expediat',
      colorClass: 'state--shipped',
      description: 'Coletul a plecat cu curier / a fost depus în easybox. AWB-ul este înregistrat.',
    },
    {
      id: 'Delivered',
      label: 'Livrat',
      colorClass: 'state--delivered',
      description: 'Clientul a confirmat sau curiorul a marcat livrarea. Status final pozitiv.',
      isTerminal: true,
    },
    {
      id: 'PaymentFailed',
      label: 'Plată eșuată',
      colorClass: 'state--failed',
      description: 'Webhook-ul de plată a raportat eșec. Clientul poate relua plata din contul său.',
      isTerminal: true,
    },
    {
      id: 'Cancelled',
      label: 'Anulat',
      colorClass: 'state--cancelled',
      description: 'Comanda a fost anulată de admin. Rambursarea este inițiată automat prin Stripe / EuPlatesc.',
      isTerminal: true,
    },
  ];

  readonly transitions: Transition[] = [
    { from: 'AwaitingPayment', to: 'Paid',            trigger: 'Webhook plată confirmat',       actor: 'system' },
    { from: 'AwaitingPayment', to: 'PaymentFailed',   trigger: 'Webhook plată eșuată',          actor: 'system' },
    { from: 'Paid',            to: 'Printing',        trigger: 'Admin marchează „Printare"',     actor: 'admin'  },
    { from: 'Paid',            to: 'Cancelled',       trigger: 'Admin anulează comanda',         actor: 'admin'  },
    { from: 'Printing',        to: 'Shipped',         trigger: 'Admin adaugă AWB + expediază',   actor: 'admin'  },
    { from: 'Printing',        to: 'Cancelled',       trigger: 'Admin anulează comanda',         actor: 'admin'  },
    { from: 'Shipped',         to: 'Delivered',       trigger: 'Admin marchează „Livrat"',       actor: 'admin'  },
  ];

  readonly rules: string[] = [
    'Tranzițiile sunt validate server-side prin OrderStatusMachine.cs — orice tranziție invalidă returnează 400. Marcarea manuală ca Plătit returnează 409 dacă nu se poate aloca un număr de factură.',
    'Statusurile terminale (Livrat, Anulat, Plată eșuată) nu pot fi modificate ulterior.',
    'Anularea unui ordin Plătit sau În printare declanșează automat rambursarea prin procesatorul de plată.',
    'Tranziția la Expediat necesită obligatoriu un număr AWB. URL de tracking este opțional.',
    'AwaitingPayment → Paid și AwaitingPayment → PaymentFailed sunt declanșate de webhook-uri. Marcarea manuală ca Plătit, pentru reconciliere offline, există doar prin API — panoul nu are buton pentru ea.',
    'Notificările email sunt trimise automat la: Confirmare comandă, Expediat, Livrat, Anulat. Confirmarea nu se retrimite dacă o altă livrare a emis deja factura comenzii.',
  ];

  stateLabel(id: string): string {
    return this.states.find(s => s.id === id)?.label ?? id;
  }

  stateClass(id: string): string {
    return this.states.find(s => s.id === id)?.colorClass ?? '';
  }
}
