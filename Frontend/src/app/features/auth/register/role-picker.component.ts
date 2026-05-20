import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideLeaf, LucideUser, LucideStore } from '@lucide/angular';
import { AuthLeftPanelComponent } from '../../../layouts/auth-layout/auth-left-panel.component';

@Component({
  selector: 'app-role-picker',
  standalone: true,
  imports: [RouterLink, LucideLeaf, LucideUser, LucideStore, AuthLeftPanelComponent],
  templateUrl: './role-picker.component.html'
})
export class RolePickerComponent {}
