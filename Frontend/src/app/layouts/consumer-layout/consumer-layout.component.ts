import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { LucideLeaf, LucideHome, LucideShoppingBag, LucideUser, LucideLogOut } from '@lucide/angular';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-consumer-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideLeaf, LucideHome, LucideShoppingBag, LucideUser, LucideLogOut],
  templateUrl: './consumer-layout.component.html'
})
export class ConsumerLayoutComponent {
  private readonly auth = inject(AuthService);
  logout(): void { this.auth.logout(); }
}
