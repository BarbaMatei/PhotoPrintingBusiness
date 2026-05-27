import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
} from '@angular/core';
import { Subscription } from 'rxjs';
import { Chart, registerables } from 'chart.js';
import { AdminService } from '../../../core/services/admin.service';
import { AdminHubService } from '../../../core/services/admin-hub.service';
import { AdminStatsDto, RevenueDataPointDto, OrdersByStatusDto, ProductStatsDto } from '../../../core/models/admin.model';
import { STATUS_LABELS } from '../../../core/models/order-status.constants';

Chart.register(...registerables);

@Component({
  selector: 'app-admin-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.scss',
})
export class AdminPage implements OnInit, AfterViewInit, OnDestroy {
  private readonly adminSvc = inject(AdminService);
  private readonly hubSvc = inject(AdminHubService);
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild('revenueChart') revenueCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('statusChart') statusCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('productChart') productCanvas!: ElementRef<HTMLCanvasElement>;

  stats: AdminStatsDto | null = null;
  loading = true;
  error: string | null = null;

  private revenueData: RevenueDataPointDto[] = [];
  private statusData: OrdersByStatusDto[] = [];
  private productData: ProductStatsDto[] = [];

  private revenueChartInstance: Chart | null = null;
  private statusChartInstance: Chart | null = null;
  private productChartInstance: Chart | null = null;

  private subs = new Subscription();

  readonly statusLabel = (s: string) => STATUS_LABELS[s] ?? s;

  ngOnInit(): void {
    this.loadAll();

    this.subs.add(
      this.hubSvc.newOrderReceived$.subscribe(() => {
        this.loadAll();
      })
    );

    this.hubSvc.connect().catch(err =>
      console.warn('Admin hub connect failed:', err)
    );
  }

  ngAfterViewInit(): void {
    this.initCharts();
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    this.revenueChartInstance?.destroy();
    this.statusChartInstance?.destroy();
    this.productChartInstance?.destroy();
  }

  private loadAll(): void {
    this.loading = true;
    this.error = null;
    this.cdr.markForCheck();

    let remaining = 4;
    const done = () => {
      remaining--;
      if (remaining === 0) {
        this.loading = false;
        this.updateCharts();
        this.cdr.markForCheck();
      }
    };

    this.adminSvc.getStats().subscribe({
      next: s => { this.stats = s; done(); },
      error: () => { this.error = 'Eroare la încărcarea statisticilor.'; done(); },
    });

    this.adminSvc.getRevenueChart(30).subscribe({
      next: d => { this.revenueData = d; done(); },
      error: () => done(),
    });

    this.adminSvc.getOrdersByStatus().subscribe({
      next: d => { this.statusData = d; done(); },
      error: () => done(),
    });

    this.adminSvc.getProductStats().subscribe({
      next: d => { this.productData = d; done(); },
      error: () => done(),
    });
  }

  private initCharts(): void {
    if (this.revenueCanvas) {
      this.revenueChartInstance = new Chart(this.revenueCanvas.nativeElement, {
        type: 'line',
        data: { labels: [], datasets: [{ label: 'Venituri (RON)', data: [], borderColor: '#3b82f6', tension: 0.4, fill: true, backgroundColor: 'rgba(59,130,246,0.1)' }] },
        options: { responsive: true, plugins: { legend: { display: false } } },
      });
    }
    if (this.statusCanvas) {
      this.statusChartInstance = new Chart(this.statusCanvas.nativeElement, {
        type: 'bar',
        data: { labels: [], datasets: [{ label: 'Comenzi', data: [], backgroundColor: '#6366f1' }] },
        options: { responsive: true, plugins: { legend: { display: false } } },
      });
    }
    if (this.productCanvas) {
      this.productChartInstance = new Chart(this.productCanvas.nativeElement, {
        type: 'doughnut',
        data: { labels: [], datasets: [{ data: [], backgroundColor: ['#3b82f6','#6366f1','#8b5cf6','#a78bfa','#c4b5fd','#ddd6fe','#ede9fe','#f5f3ff','#f0fdf4','#dcfce7'] }] },
        options: { responsive: true },
      });
    }
    this.updateCharts();
  }

  private updateCharts(): void {
    if (this.revenueChartInstance && this.revenueData.length) {
      this.revenueChartInstance.data.labels = this.revenueData.map(d => d.date);
      this.revenueChartInstance.data.datasets[0].data = this.revenueData.map(d => d.revenue);
      this.revenueChartInstance.update();
    }

    if (this.statusChartInstance && this.statusData.length) {
      this.statusChartInstance.data.labels = this.statusData.map(d => this.statusLabel(d.status));
      this.statusChartInstance.data.datasets[0].data = this.statusData.map(d => d.count);
      this.statusChartInstance.update();
    }

    if (this.productChartInstance && this.productData.length) {
      this.productChartInstance.data.labels = this.productData.map(d => d.productName);
      this.productChartInstance.data.datasets[0].data = this.productData.map(d => d.totalQuantity);
      this.productChartInstance.update();
    }
  }

  formatRon(value: number): string {
    return value.toFixed(2).replace('.', ',') + ' RON';
  }
}
