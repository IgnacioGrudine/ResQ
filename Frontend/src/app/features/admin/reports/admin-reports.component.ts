import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { AdminMerchantListItem, ReportFormat } from '../../../core/models/admin.models';
import { ResqSelectComponent } from '../../../shared/ui/select/resq-select.component';
import { ResqOptionComponent } from '../../../shared/ui/select/resq-option.component';
import { LucideFileText, LucideTrophy, LucideStore } from '@lucide/angular';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [FormsModule, ResqSelectComponent, ResqOptionComponent, LucideFileText, LucideTrophy, LucideStore],
  templateUrl: './admin-reports.component.html'
})
export class AdminReportsComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly merchants = signal<AdminMerchantListItem[]>([]);

  from = isoDaysAgo(30);
  to   = isoToday();
  selectedMerchantId: number | null = null;

  ngOnInit(): void {
    this.admin.getMerchants({ pageSize: 100 }).subscribe({
      next: p => {
        this.merchants.set(p.items);
        this.selectedMerchantId = p.items[0]?.id ?? null;
      }
    });
  }

  global(format: ReportFormat): void {
    this.admin.downloadGlobalReport(this.from, this.to, format);
  }

  ranking(format: ReportFormat): void {
    this.admin.downloadRankingReport(this.from, this.to, format);
  }

  byMerchant(format: ReportFormat): void {
    if (this.selectedMerchantId) {
      this.admin.downloadMerchantReport(this.selectedMerchantId, this.from, this.to, format);
    }
  }
}

function isoToday(): string {
  return new Date().toISOString().slice(0, 10);
}

function isoDaysAgo(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}
