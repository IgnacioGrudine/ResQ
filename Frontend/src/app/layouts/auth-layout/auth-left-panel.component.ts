import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideLeaf, LucideSprout, LucideShoppingBag, LucideStore } from '@lucide/angular';

@Component({
  selector: 'app-auth-left-panel',
  standalone: true,
  imports: [RouterLink, LucideLeaf, LucideSprout, LucideShoppingBag, LucideStore],
  templateUrl: './auth-left-panel.component.html'
})
export class AuthLeftPanelComponent {}
