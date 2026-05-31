import { Component, ElementRef, ViewChild, inject, signal, AfterViewInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideLeaf, LucideStore, LucideTriangleAlert, LucideCheck, LucideMapPin, LucideCamera } from '@lucide/angular';
import { AuthService } from '../../../../core/services/auth.service';
import { environment } from '../../../../../environments/environment';
import { MerchantService } from '../../../../core/services/merchant.service';
import { AuthLeftPanelComponent } from '../../../../layouts/auth-layout/auth-left-panel.component';
import { ResqButtonComponent } from '../../../../shared/ui/button/resq-button.component';
import { ResqInputComponent } from '../../../../shared/ui/input/resq-input.component';

const CUIT_PATTERN  = /^\d{2}-\d{8}-\d{1}$/;
const PHONE_PATTERN = /^\+?[\d\s\-()+]{7,20}$/;

/** Rechaza el valor 0 — la coordenada por defecto antes de confirmar con Google */
function nonZeroValidator(control: { value: number }) {
  return control.value === 0 ? { nonZero: true } : null;
}

@Component({
  selector: 'app-register-merchant',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, LucideLeaf, LucideStore, LucideTriangleAlert, LucideCheck, LucideMapPin, LucideCamera, AuthLeftPanelComponent, ResqButtonComponent, ResqInputComponent],
  templateUrl: './register-merchant.component.html'
})
export class RegisterMerchantComponent implements AfterViewInit {
  private readonly fb              = inject(FormBuilder);
  private readonly authService     = inject(AuthService);
  private readonly merchantService = inject(MerchantService);
  private readonly router          = inject(Router);

  @ViewChild('photoInput')  photoInput!:  ElementRef<HTMLInputElement>;
  @ViewChild('addressInput') addressInput!: ElementRef<HTMLInputElement>;

  readonly loading      = signal(false);
  readonly apiError     = signal('');
  readonly geoDetected  = signal(false);   // true cuando el usuario selecciona una sugerencia de Places
  readonly photoPreview = signal<string | null>(null);
  pendingPhoto: File | null = null;

  readonly form = this.fb.group({
    email:        ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    password:     ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    businessName: ['', [Validators.required, Validators.maxLength(150)]],
    cuit:         ['', [Validators.required, Validators.pattern(CUIT_PATTERN)]],
    address:      ['', [Validators.required, Validators.maxLength(255)]],
    contactPhone: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    latitude:     [0 as number, [Validators.required, Validators.min(-90),   Validators.max(90),  nonZeroValidator]],
    longitude:    [0 as number, [Validators.required, Validators.min(-180),  Validators.max(180), nonZeroValidator]]
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

  triggerPhotoInput(): void {
    this.photoInput.nativeElement.click();
  }

  onPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.pendingPhoto = file;
    const reader = new FileReader();
    reader.onload = () => this.photoPreview.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  ngAfterViewInit(): void {
    this.initPlacesAutocomplete();
  }

  private initPlacesAutocomplete(): void {
    // Si ya está cargado el SDK, usarlo directo
    if ((window as any).google?.maps?.places?.Autocomplete) {
      this.setupAutocomplete();
      return;
    }

    // Si el script ya fue inyectado por otro componente, esperar a que termine
    const existing = document.querySelector<HTMLScriptElement>('script[data-resq-maps]');
    if (existing) {
      existing.addEventListener('load', () => this.setupAutocomplete(), { once: true });
      return;
    }

    // Inyectar el script del SDK — usamos carga sync con libraries=places
    // (sin loading=async porque hace que la auth de la key falle al hacer ref a localhost)
    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${environment.googleMapsApiKey}&libraries=places&v=weekly`;
    script.async = true;
    script.defer = true;
    script.dataset['resqMaps'] = 'true';
    script.onload = () => this.setupAutocomplete();
    document.head.appendChild(script);
  }

  private setupAutocomplete(): void {
    if (!(window as any).google?.maps?.places?.Autocomplete) {
      console.warn('[ResQ] Google Maps Places no disponible. Verificá que Maps JavaScript API y Places API estén habilitadas.');
      return;
    }
    const input = this.addressInput.nativeElement;
    const autocomplete = new (window as any).google.maps.places.Autocomplete(input, {
      types: ['address'],
      componentRestrictions: { country: 'ar' }   // Solo Argentina
    });

    // Si el usuario edita a mano después de confirmar, resetear el estado
    input.addEventListener('input', () => {
      if (this.geoDetected()) this.geoDetected.set(false);
    });

    autocomplete.addListener('place_changed', () => {
      const place = autocomplete.getPlace();
      if (!place.geometry?.location) return;
      this.form.patchValue({
        address:   place.formatted_address ?? '',
        latitude:  place.geometry.location.lat(),
        longitude: place.geometry.location.lng()
      });
      this.geoDetected.set(true);
    });
  }

  resetAddress(): void {
    this.geoDetected.set(false);
    this.form.patchValue({ address: '', latitude: 0, longitude: 0 });
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
        if (this.pendingPhoto) {
          this.merchantService.uploadPhoto(this.pendingPhoto).subscribe({
            next:  () => { this.loading.set(false); this.router.navigate(['/panel']); },
            error: () => { this.loading.set(false); this.router.navigate(['/panel']); }
          });
        } else {
          this.loading.set(false);
          this.router.navigate(['/panel']);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.apiError.set(err.error?.detail ?? 'Error al registrarse. Intentá de nuevo.');
      }
    });
  }
}
