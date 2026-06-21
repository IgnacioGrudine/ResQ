import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import {
  LucideLeaf,
  LucideLayoutDashboard,
  LucidePackage,
  LucideClipboardList,
  LucideStar,
  LucideStore,
  LucideTrendingUp,
  LucideLogOut
} from '@lucide/angular';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-merchant-layout',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    LucideLeaf, LucideLayoutDashboard, LucidePackage, LucideClipboardList, LucideStar, LucideStore, LucideTrendingUp, LucideLogOut
  ],
  templateUrl: './merchant-layout.component.html'
})
export class MerchantLayoutComponent {
  private readonly auth = inject(AuthService);
  logout(): void { this.auth.logout(); }
}
