import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import {
  LucideLeaf,
  LucideLayoutDashboard,
  LucidePackage,
  LucideClipboardList,
  LucideStar,
  LucideStore
} from '@lucide/angular';

@Component({
  selector: 'app-merchant-layout',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    LucideLeaf, LucideLayoutDashboard, LucidePackage, LucideClipboardList, LucideStar, LucideStore
  ],
  templateUrl: './merchant-layout.component.html'
})
export class MerchantLayoutComponent {}
