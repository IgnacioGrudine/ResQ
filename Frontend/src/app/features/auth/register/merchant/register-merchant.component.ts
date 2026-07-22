import { Component, ElementRef, ViewChild, inject, signal, AfterViewInit, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import {
  LucideLeaf, LucideStore, LucideTriangleAlert, LucideCheck, LucideMapPin,
  LucideCamera, LucideLink, LucideShieldCheck, LucideWallet, LucideTag
} from '@lucide/angular';
import { AuthService } from '../../../../core/services/auth.service';
import { MerchantService } from '../../../../core/services/merchant.service';
import { CatalogService } from '../../../../core/services/catalog.service';
import { MercadoPagoService } from '../../../../core/services/mercadopago.service';
import { Category } from '../../../../core/models/catalog.models';
import { UpdateMerchantProfilePayload } from '../../../../core/models/merchant.models';
import { environment } from '../../../../../environments/environment';
import { AuthLeftPanelComponent } from '../../../../layouts/auth-layout/auth-left-panel.component';
import { ResqButtonComponent } from '../../../../shared/ui/button/resq-button.component';
import { ResqInputComponent } from '../../../../shared/ui/input/resq-input.component';

const CUIT_PATTERN  = /^\d{2}-\d{8}-\d{1}$/;
const PHONE_PATTERN = /^\+?[\d\s\-()+]{7,20}$/;

function nonZeroValidator(control: { value: number }) {
  return control.value === 0 ? { nonZero: true } : null;
}

@Component({
  selector: 'app-register-merchant',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, DecimalPipe,
    LucideLeaf, LucideStore, LucideTriangleAlert, LucideCheck, LucideMapPin,
    LucideCamera, LucideLink, LucideShieldCheck, LucideWallet, LucideTag,
    AuthLeftPanelComponent, ResqButtonComponent, ResqInputComponent
  ],
  templateUrl: './register-merchant.component.html'
})
export class RegisterMerchantComponent implements OnInit, AfterViewInit {
  private readonly fb             = inject(FormBuilder);
  private readonly authService    = inject(AuthService);
  private readonly merchantService= inject(MerchantService);
  private readonly catalogService = inject(CatalogService);
  private readonly mpService      = inject(MercadoPagoService);
  private readonly router         = inject(Router);

  @ViewChild('photoInput')   photoInput!:   ElementRef<HTMLInputElement>;
  @ViewChild('addressInput') addressInput!: ElementRef<HTMLInputElement>;

  /** Wizard step: 1 = datos, 2 = categorías, 3 = Mercado Pago. */
  readonly step         = signal<1 | 2 | 3>(1);
  readonly loading      = signal(false);
  readonly apiError     = signal('');
  readonly geoDetected  = signal(false);
  readonly photoPreview = signal<string | null>(null);
  readonly showPhotoError = signal(false);
  pendingPhoto: File | null = null;

  readonly categories    = signal<Category[]>([]);
  readonly savingCats    = signal(false);
  selectedCategoryIds    = new Set<number>();

  readonly mpConnecting = signal(false);
  readonly mpError      = signal('');

  readonly form = this.fb.group({
    email:        ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    password:     ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    businessName: ['', [Validators.required, Validators.maxLength(150)]],
    cuit:         ['', [Validators.required, Validators.pattern(CUIT_PATTERN)]],
    address:      ['', [Validators.required, Validators.maxLength(255)]],
    contactPhone: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    latitude:     [0 as number, [Validators.required, Validators.min(-90),  Validators.max(90),  nonZeroValidator]],
    longitude:    [0 as number, [Validators.required, Validators.min(-180), Validators.max(180), nonZeroValidator]]
  });

  constructor() {
    this.form.get('cuit')!.valueChanges.subscribe(val => {
      if (!val) return;
      const digits = val.replace(/\D/g, '').slice(0, 11);
      let formatted = digits;
      if (digits.length > 2)  formatted = `${digits.slice(0, 2)}-${digits.slice(2)}`;
      if (digits.length > 10) formatted = `${digits.slice(0, 2)}-${digits.slice(2, 10)}-${digits.slice(10)}`;
      if (formatted !== val) this.form.get('cuit')!.setValue(formatted, { emitEvent: false });
    });
  }

  ngOnInit(): void {
    this.catalogService.getCategories().subscribe(cats => this.categories.set(cats));
  }

  ngAfterViewInit(): void {
    this.initPlacesAutocomplete();
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

  triggerPhotoInput(): void { this.photoInput.nativeElement.click(); }

  onPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.pendingPhoto = file;
    this.showPhotoError.set(false);
    const reader = new FileReader();
    reader.onload = () => this.photoPreview.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  private initPlacesAutocomplete(): void {
    if ((window as any).google?.maps?.places?.Autocomplete) {
      this.setupAutocomplete();
      return;
    }
    const existing = document.querySelector<HTMLScriptElement>('script[data-resq-maps]');
    if (existing) {
      existing.addEventListener('load', () => this.setupAutocomplete(), { once: true });
      return;
    }
    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${environment.googleMapsApiKey}&libraries=places&v=weekly`;
    script.async = true;
    script.defer = true;
    script.dataset['resqMaps'] = 'true';
    script.onload = () => this.setupAutocomplete();
    document.head.appendChild(script);
  }

  private setupAutocomplete(): void {
    if (!(window as any).google?.maps?.places?.Autocomplete) return;
    const input = this.addressInput.nativeElement;
    const autocomplete = new (window as any).google.maps.places.Autocomplete(input, {
      types: ['address'],
      componentRestrictions: { country: 'ar' }
    });
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

  toggleCategory(id: number): void {
    if (this.selectedCategoryIds.has(id)) this.selectedCategoryIds.delete(id);
    else this.selectedCategoryIds.add(id);
  }

  isCatSelected(id: number): boolean { return this.selectedCategoryIds.has(id); }

  onSubmit(): void {
    const missingPhoto = !this.pendingPhoto;
    this.showPhotoError.set(missingPhoto);
    if (this.form.invalid || missingPhoto) { this.form.markAllAsTouched(); return; }
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
        this.merchantService.uploadPhoto(this.pendingPhoto!).subscribe({
          next:  () => this.goToCatStep(),
          error: () => this.goToCatStep()
        });
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.apiError.set(err.error?.detail ?? 'Error al registrarse. Intentá de nuevo.');
      }
    });
  }

  private goToCatStep(): void {
    this.loading.set(false);
    this.step.set(2);
  }

  saveCategoriesAndContinue(): void {
    this.savingCats.set(true);
    const v = this.form.getRawValue();
    const payload: UpdateMerchantProfilePayload = {
      businessName: v.businessName!,
      address:      v.address!,
      contactPhone: v.contactPhone!,
      latitude:     v.latitude!,
      longitude:    v.longitude!,
      categoryIds:  [...this.selectedCategoryIds]
    };
    this.merchantService.updateProfile(payload).subscribe({
      next:  () => { this.savingCats.set(false); this.step.set(3); },
      error: () => { this.savingCats.set(false); this.step.set(3); }
    });
  }

  connectMp(): void {
    this.mpConnecting.set(true);
    this.mpError.set('');
    this.mpService.getAuthUrl().subscribe({
      next: ({ authUrl }) => { window.location.href = authUrl; },
      error: () => {
        this.mpConnecting.set(false);
        this.mpError.set('No se pudo iniciar la conexión con Mercado Pago. Intentá de nuevo.');
      }
    });
  }

  skipForNow(): void { this.router.navigate(['/panel']); }
}
