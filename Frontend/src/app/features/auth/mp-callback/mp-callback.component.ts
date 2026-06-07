import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideCheckCircle, LucideXCircle } from '@lucide/angular';

type CallbackState = 'loading' | 'success' | 'error';

@Component({
  selector: 'app-mp-callback',
  standalone: true,
  imports: [LucideCheckCircle, LucideXCircle],
  template: `
    <div class="min-h-screen bg-gray-50 flex items-center justify-center px-4">
      <div class="bg-white rounded-2xl shadow-sm p-8 max-w-sm w-full text-center flex flex-col items-center gap-5">

        @if (state() === 'loading') {
          <div class="w-14 h-14 border-4 border-evergreen/20 border-t-evergreen rounded-full animate-spin"></div>
          <p class="text-gray-500 text-sm">Procesando vinculación...</p>
        }

        @else if (state() === 'success') {
          <div class="w-16 h-16 rounded-full bg-lime/60 flex items-center justify-center">
            <svg lucideCheckCircle class="w-8 h-8 text-hunter"></svg>
          </div>
          <div>
            <h1 class="text-xl font-bold text-gray-800">¡Vinculación exitosa!</h1>
            <p class="text-sm text-gray-500 mt-1">Tu cuenta de Mercado Pago fue conectada correctamente.</p>
          </div>
          <p class="text-xs text-gray-400">Redirigiendo al panel...</p>
        }

        @else {
          <div class="w-16 h-16 rounded-full bg-red-50 flex items-center justify-center">
            <svg lucideXCircle class="w-8 h-8 text-red-500"></svg>
          </div>
          <div>
            <h1 class="text-xl font-bold text-gray-800">Error al vincular</h1>
            <p class="text-sm text-gray-500 mt-1">{{ errorMessage() }}</p>
          </div>
          <button (click)="goToPanel()"
            class="w-full py-3 bg-evergreen text-white rounded-xl text-sm font-semibold hover:bg-hunter transition-colors">
            Volver al panel
          </button>
        }

      </div>
    </div>
  `
})
export class MpCallbackComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly state        = signal<CallbackState>('loading');
  readonly errorMessage = signal<string>('No se pudo completar la vinculación con Mercado Pago.');

  ngOnInit(): void {
    const status  = this.route.snapshot.queryParamMap.get('status');
    const message = this.route.snapshot.queryParamMap.get('message');

    if (status === 'success') {
      this.state.set('success');
      setTimeout(() => this.goToPanel(), 2500);
    } else {
      this.state.set('error');
      if (message) this.errorMessage.set(decodeURIComponent(message));
    }
  }

  goToPanel(): void {
    this.router.navigate(['/panel/profile']);
  }
}
