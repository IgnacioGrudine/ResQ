import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideLeaf, LucideTriangleAlert } from '@lucide/angular';
import { AuthService } from '../../../core/services/auth.service';
import { AuthLeftPanelComponent } from '../../../layouts/auth-layout/auth-left-panel.component';
import { ResqButtonComponent } from '../../../shared/ui/button/resq-button.component';
import { ResqInputComponent } from '../../../shared/ui/input/resq-input.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LucideLeaf, LucideTriangleAlert, AuthLeftPanelComponent, ResqButtonComponent, ResqInputComponent],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly apiError = signal('');

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

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
        this.router.navigate([res.role === 'Merchant' ? '/panel' : '/home']);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.apiError.set(err.error?.detail ?? 'Error al iniciar sesión. Intentá de nuevo.');
      }
    });
  }
}
