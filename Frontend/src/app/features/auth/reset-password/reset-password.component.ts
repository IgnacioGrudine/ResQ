import { Component, inject, signal, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideLeaf, LucideCheckCircle, LucideTriangleAlert } from '@lucide/angular';
import { AuthService } from '../../../core/services/auth.service';
import { AuthLeftPanelComponent } from '../../../layouts/auth-layout/auth-left-panel.component';
import { ResqButtonComponent } from '../../../shared/ui/button/resq-button.component';
import { ResqInputComponent } from '../../../shared/ui/input/resq-input.component';

function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('newPassword')?.value;
  const confirm   = control.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { passwordMismatch: true } : null;
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LucideLeaf, LucideCheckCircle, LucideTriangleAlert, AuthLeftPanelComponent, ResqButtonComponent, ResqInputComponent],
  templateUrl: './reset-password.component.html'
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private token = '';

  readonly loading = signal(false);
  readonly success = signal(false);
  readonly apiError = signal('');
  readonly missingToken = signal(false);

  readonly form = this.fb.group({
    newPassword:     ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: passwordsMatchValidator });

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.token) this.missingToken.set(true);
  }

  fieldError(field: string): string {
    const ctrl = this.form.get(field);
    if (!ctrl?.invalid || !ctrl.touched) return '';
    if (ctrl.hasError('required')) return 'Este campo es requerido.';
    if (ctrl.hasError('minlength')) return 'La contraseña debe tener al menos 8 caracteres.';
    return '';
  }

  get confirmError(): string {
    const ctrl = this.form.get('confirmPassword');
    if (!ctrl?.touched) return '';
    if (ctrl.hasError('required')) return 'Este campo es requerido.';
    if (this.form.hasError('passwordMismatch')) return 'Las contraseñas no coinciden.';
    return '';
  }

  onSubmit(): void {
    if (this.form.invalid || this.missingToken()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.apiError.set('');

    const { newPassword } = this.form.getRawValue();
    this.authService.resetPassword({ token: this.token, newPassword: newPassword! }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.apiError.set(err.error?.detail ?? 'No se pudo restablecer la contraseña. Intentá de nuevo.');
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
