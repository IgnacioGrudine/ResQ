import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideLeaf, LucideUser, LucideTriangleAlert } from '@lucide/angular';
import { AuthService } from '../../../../core/services/auth.service';
import { ResqButtonComponent } from '../../../../shared/ui/button/resq-button.component';
import { ResqInputComponent } from '../../../../shared/ui/input/resq-input.component';

@Component({
  selector: 'app-register-consumer',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LucideLeaf, LucideUser, LucideTriangleAlert, ResqButtonComponent, ResqInputComponent],
  templateUrl: './register-consumer.component.html'
})
export class RegisterConsumerComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly apiError = signal('');

  readonly form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName:  ['', [Validators.required, Validators.maxLength(100)]],
    email:     ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    password:  ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    phoneNumber: ['', [Validators.pattern(/^\+?[\d\s\-()+]{7,20}$/)]]
  });

  fieldError(field: string): string {
    const ctrl = this.form.get(field);
    if (!ctrl?.invalid || !ctrl.touched) return '';
    if (ctrl.hasError('required'))   return 'Este campo es requerido.';
    if (ctrl.hasError('email'))      return 'El email no tiene un formato válido.';
    if (ctrl.hasError('minlength'))  return 'La contraseña debe tener al menos 8 caracteres.';
    if (ctrl.hasError('maxlength'))  return 'El valor ingresado es demasiado largo.';
    if (ctrl.hasError('pattern'))    return 'El teléfono no tiene un formato válido.';
    return '';
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.apiError.set('');

    const v = this.form.getRawValue();
    this.authService.registerConsumer({
      email:       v.email!,
      password:    v.password!,
      firstName:   v.firstName!,
      lastName:    v.lastName!,
      phoneNumber: v.phoneNumber || undefined
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/home']);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.apiError.set(err.error?.detail ?? 'Error al registrarse. Intentá de nuevo.');
      }
    });
  }
}
