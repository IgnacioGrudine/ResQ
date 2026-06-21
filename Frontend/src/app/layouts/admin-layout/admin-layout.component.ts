import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import {
  LucideLeaf,
  LucideLayoutDashboard,
  LucideStore,
  LucideUsers,
  LucideFileText,
  LucideLogOut
} from '@lucide/angular';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    LucideLeaf, LucideLayoutDashboard, LucideStore, LucideUsers, LucideFileText, LucideLogOut
  ],
  templateUrl: './admin-layout.component.html'
})
export class AdminLayoutComponent {
  private readonly auth = inject(AuthService);
  logout(): void { this.auth.logout(); }
}
