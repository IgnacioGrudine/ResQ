import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideLeaf, LucideStore, LucideTriangleAlert, LucideCheck, LucideMapPin } from '@lucide/angular';
import { AuthService } from '../../../../core/services/auth.service';
import { AuthLeftPanelComponent } from '../../../../layouts/auth-layout/auth-left-panel.component';
import { ResqButtonComponent } from '../../../../shared/ui/button/resq-button.component';
import { ResqInputComponent } from '../../../../shared/ui/input/resq-input.component';

const CUIT_PATTERN = /^\d{2}-\d{8}-\d{1}$/;
const PHONE_PATTERN = /^\+?[\d\s\-()+]{7,20}$/;

@Component({
  selector: 'app-register-merchant',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, LucideLeaf, LucideStore, LucideTriangleAlert, LucideCheck, LucideMapPin, AuthLeftPanelComponent, ResqButtonComponent, ResqInputComponent],
  templateUrl: './register-merchant.component.html'
})
export class RegisterMerchantComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly apiError = signal('');
  readonly geoLoading = signal(false);
  readonly geoError = signal('');
  readonly geoDetected = signal(false);

  readonly form = this.fb.group({
    email:        ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    password:     ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    businessName: ['', [Validators.required, Validators.maxLength(150)]],
    cuit:         ['', [Validators.required, Validators.pattern(CUIT_PATTERN)]],
    address:      ['', [Validators.required, Validators.maxLength(255)]],
    contactPhone: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    latitude:     [0 as number, [Validators.required, Validators.min(-90), Validators.max(90)]],
    longitude:    [0 as number, [Validators.required, Validators.min(-180), Validators.max(180)]]
  });

  constructor() {
    // Auto-format CUIT as XX-XXXXXXXX-X while user types
    this.form.get('cuit')!.valueChanges.subscribe(val => {
      if (!val) return;
      const digits = val.replace(/\D/g, '').slice(0, 11);
      let formatted = digits;
      if (digits.length > 2)  formatted = `${digits.slice(0, 2)}-${digits.slice(2)}`;
      if (digits.length > 10) formatted = `${digits.slice(0, 2)}-${digits.slice(2, 10)}-${digits.slice(10)}`;
      if (formatted !== val) this.form.get('cuit')!.setValue(formatted, { emitEvent: false });
    });
  }

  fieldError(field: string): string {
    const ctrl = this.form.get(field);
    if (!ctrl?.invalid || !ctrl.touched) return '';
    if (ctrl.hasError('required'))  return 'Este campo es requerido.';
    if (ctrl.hasError('email'))     return 'El email no tiene un formato válido.';
    if (ctrl.hasError('minlength')) return 'La contraseña debe tener al menos 8 caracteres.';
    if (ctrl.hasError('maxlength')) return 'El valor ingresado es demasiado largo.';
    if (ctrl.hasError('pattern') && field === 'cuit')
      return 'El CUIT debe tener el formato XX-XXXXXXXX-X.';
    if (ctrl.hasError('pattern') && field === 'contactPhone')
      return 'El teléfono no tiene un formato válido.';
    return '';
  }

  detectLocation(): void {
    if (!navigator.geolocation) {
      this.geoError.set('Tu navegador no soporta geolocalización.');
      return;
    }

    this.geoLoading.set(true);
    this.geoError.set('');

    navigator.geolocation.getCurrentPosition(
      pos => {
        this.form.patchValue({
          latitude:  pos.coords.latitude,
          longitude: pos.coords.longitude
        });
        this.geoDetected.set(true);
        this.geoLoading.set(false);
      },
      () => {
        this.geoError.set('No se pudo obtener la ubicación. Asegurate de dar permiso al navegador.');
        this.geoLoading.set(false);
      },
      { timeout: 10000 }
    );
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.apiError.set('');

    const v = this.form.getRawValue();
    this.authService.registerMerchant({
      email:        v.email!,
      password:     v.password!,
      businessName: v.businessName!,
      cuit:         v.cuit!,
      address:      v.address!,
      contactPhone: v.contactPhone!,
      latitude:     v.latitude!,
      longitude:    v.longitude!
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/panel']);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.apiError.set(err.error?.detail ?? 'Error al registrarse. Intentá de nuevo.');
      }
    });
  }
}
