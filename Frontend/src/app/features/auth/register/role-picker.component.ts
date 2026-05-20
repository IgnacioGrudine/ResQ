import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideLeaf, LucideUser, LucideStore } from '@lucide/angular';

@Component({
  selector: 'app-role-picker',
  standalone: true,
  imports: [RouterLink, LucideLeaf, LucideUser, LucideStore],
  templateUrl: './role-picker.component.html'
})
export class RolePickerComponent {}
