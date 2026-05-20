import { Component, inject } from '@angular/core';
import { LucideLeaf } from '@lucide/angular';
import { AuthService } from '../../core/services/auth.service';
import { ResqButtonComponent } from '../../shared/ui/button/resq-button.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [LucideLeaf, ResqButtonComponent],
  template: `
    <div class="min-h-screen bg-lime flex flex-col items-center justify-center gap-6 px-4">
      <svg lucideLeaf class="w-16 h-16 text-evergreen"></svg>
      <h1 class="text-3xl font-bold text-evergreen">¡Bienvenido a ResQ!</h1>
      <p class="text-hunter text-lg">Rol: <strong>{{ auth.role() }}</strong></p>
      <p class="text-gray-500 text-sm">Esta pantalla será reemplazada en el próximo sprint.</p>
      <resq-button variant="ghost" (clicked)="auth.logout()">Cerrar sesión</resq-button>
    </div>
  `
})
export class HomeComponent {
  readonly auth = inject(AuthService);
}
