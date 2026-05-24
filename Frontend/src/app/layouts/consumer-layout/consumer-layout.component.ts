import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { LucideLeaf, LucideHome, LucideShoppingBag, LucideUser } from '@lucide/angular';

@Component({
  selector: 'app-consumer-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideLeaf, LucideHome, LucideShoppingBag, LucideUser],
  templateUrl: './consumer-layout.component.html'
})
export class ConsumerLayoutComponent {}
