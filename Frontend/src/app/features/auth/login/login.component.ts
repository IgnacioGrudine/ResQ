import { Component, inject, signal, AfterViewInit, ElementRef, ViewChild, NgZone } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideLeaf, LucideTriangleAlert } from '@lucide/angular';
import { AuthService } from '../../../core/services/auth.service';
import { AuthLeftPanelComponent } from '../../../layouts/auth-layout/auth-left-panel.component';
import { ResqButtonComponent } from '../../../shared/ui/button/resq-button.component';
import { ResqInputComponent } from '../../../shared/ui/input/resq-input.component';

declare const google: any;

const GOOGLE_CLIENT_ID = '1057032626071-0rv6obmuvhnh2457rj1mqfvc8kd73a92.apps.googleusercontent.com';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LucideLeaf, LucideTriangleAlert, AuthLeftPanelComponent, ResqButtonComponent, ResqInputComponent],
  templateUrl: './login.component.html'
})
export class LoginComponent implements AfterViewInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly ngZone = inject(NgZone);

  @ViewChild('googleBtn') googleBtn!: ElementRef;

  readonly loading = signal(false);
  readonly apiError = signal('');
  readonly googleLoading = signal(false);
  readonly googleError = signal('');

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  ngAfterViewInit(): void {
    if (typeof google !== 'undefined' && google.accounts) {
      this.initGoogle();
    } else {
      window.addEventListener('load', () => {
        this.ngZone.run(() => {
          if (typeof google !== 'undefined') this.initGoogle();
        });
      }, { once: true });
    }
  }

  private initGoogle(): void {
    google.accounts.id.initialize({
      client_id: GOOGLE_CLIENT_ID,
      callback: (res: { credential: string }) =>
        this.ngZone.run(() => this.handleGoogleCallback(res))
    });

    // GSI renders a fixed-width iframe — clamp it to the card's actual content
    // width so it doesn't overflow narrow mobile viewports (368 is only safe
    // on the desktop-width card).
    const containerWidth = this.googleBtn.nativeElement.parentElement?.clientWidth;
    const buttonWidth = containerWidth ? Math.min(368, containerWidth) : 368;

    google.accounts.id.renderButton(this.googleBtn.nativeElement, {
      type: 'standard',
      shape: 'rectangular',
      theme: 'outline',
      text: 'continue_with',
      size: 'large',
      locale: 'es',
      width: buttonWidth
    });
  }

  private handleGoogleCallback(response: { credential: string }): void {
    this.googleLoading.set(true);
    this.googleError.set('');
    this.authService.loginWithGoogle(response.credential).subscribe({
      next: res => {
        this.googleLoading.set(false);
        const target = res.role === 'Admin' ? '/admin' : res.role === 'Merchant' ? '/panel' : '/home';
        this.router.navigate([target]);
      },
      error: (err: HttpErrorResponse) => {
        this.googleLoading.set(false);
        this.googleError.set(err.error?.detail ?? 'Error al iniciar sesión con Google. Intentá de nuevo.');
      }
    });
  }

  fieldError(field: string): string {
    const ctrl = this.form.get(field);
    if (!ctrl?.invalid || !ctrl.touched) return '';
    if (ctrl.hasError('required')) return 'Este campo es requerido.';
    if (ctrl.hasError('email')) return 'El email no tiene un formato válido.';
    if (ctrl.hasError('minlength')) return 'La contraseña debe tener al menos 8 caracteres.';
    return '';
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.apiError.set('');

    const { email, password } = this.form.getRawValue();
    this.authService.login({ email: email!, password: password! }).subscribe({
      next: res => {
        this.loading.set(false);
        const target = res.role === 'Admin' ? '/admin' : res.role === 'Merchant' ? '/panel' : '/home';
        this.router.navigate([target]);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.apiError.set(err.error?.detail ?? 'Error al iniciar sesión. Intentá de nuevo.');
      }
    });
  }
}
