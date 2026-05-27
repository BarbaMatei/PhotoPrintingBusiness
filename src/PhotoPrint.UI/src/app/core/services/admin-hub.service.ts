import { Injectable, OnDestroy, inject } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { NewOrderEvent } from '../models/admin.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminHubService implements OnDestroy {
  private readonly auth = inject(AuthService);

  private connection: signalR.HubConnection | null = null;

  readonly newOrderReceived$ = new Subject<NewOrderEvent>();
  readonly orderStatusChanged$ = new Subject<{ orderId: string; status: string }>();

  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) return;

    const hubUrl = this.resolveHubUrl();

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.auth.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('NewOrderReceived', (event: NewOrderEvent) => {
      this.newOrderReceived$.next(event);
    });

    this.connection.on('OrderStatusChanged', (orderId: string, status: string) => {
      this.orderStatusChanged$.next({ orderId, status });
    });

    await this.connection.start();
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  ngOnDestroy(): void {
    this.disconnect();
  }

  private resolveHubUrl(): string {
    // Strip /api from apiUrl and append the hub path
    return `${environment.apiUrl.replace(/\/api$/, '')}/hubs/admin-orders`;
  }
}
