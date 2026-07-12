import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideLeaf, LucideMailCheck } from '@lucide/angular';
import { AuthService } from '../../../core/services/auth.service';
import { AuthLeftPanelComponent } from '../../../layouts/auth-layout/auth-left-panel.component';
import { ResqButtonComponent } from '../../../shared/ui/button/resq-button.component';
import { ResqInputComponent } from '../../../shared/ui/input/resq-input.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LucideLeaf, LucideMailCheck, AuthLeftPanelComponent, ResqButtonComponent, ResqInputComponent],
  templateUrl: './forgot-password.component.html'
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  readonly loading = signal(false);
  readonly sent = signal(false);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  fieldError(field: string): string {
    const ctrl = this.form.get(field);
    if (!ctrl?.invalid || !ctrl.touched) return '';
    if (ctrl.hasError('required')) return 'Este campo es requerido.';
    if (ctrl.hasError('email')) return 'El email no tiene un formato válido.';
    return '';
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    const { email } = this.form.getRawValue();
    this.authService.forgotPassword({ email: email! }).subscribe({
      // Always show the generic confirmation, success or error, so the response
      // never reveals whether the email is actually registered.
      next:     () => { this.loading.set(false); this.sent.set(true); },
      error:    () => { this.loading.set(false); this.sent.set(true); }
    });
  }
}
