import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { AuthService } from '../../../core/services/auth.service';
import { MercadoPagoService } from '../../../core/services/mercadopago.service';
import { MerchantProfile, MerchantDashboard, UpdateMerchantProfilePayload } from '../../../core/models/merchant.models';
import { Category } from '../../../core/models/catalog.models';
import { environment } from '../../../../environments/environment';
import {
  LucideStore, LucideSave, LucideLeaf, LucideCheck, LucideCamera,
  LucideLink, LucideLink2Off, LucideAlertCircle,
  LucideTag, LucideWallet, LucideStar, LucidePackage
} from '@lucide/angular';

type Tab = 'perfil' | 'categorias' | 'pagos';

@Component({
  selector: 'app-merchant-profile',
  standalone: true,
  imports: [
    DecimalPipe, FormsModule,
    LucideStore, LucideSave, LucideLeaf, LucideCheck, LucideCamera,
    LucideLink, LucideLink2Off, LucideAlertCircle,
    LucideTag, LucideWallet, LucideStar, LucidePackage
  ],
  templateUrl: './merchant-profile.component.html'
})
export class MerchantProfileComponent implements OnInit {
  private readonly merchant  = inject(MerchantService);
  private readonly catalog   = inject(CatalogService);
  private readonly auth      = inject(AuthService);
  private readonly mpService = inject(MercadoPagoService);

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  private addressInputEl?: HTMLInputElement;
  @ViewChild('addressInput') set addressInputRef(ref: ElementRef<HTMLInputElement> | undefined) {
    if (ref?.nativeElement && this.addressInputEl !== ref.nativeElement) {
      this.addressInputEl = ref.nativeElement;
      this.initPlacesAutocomplete();
    }
  }

  readonly profile    = signal<MerchantProfile | null>(null);
  readonly dashboard  = signal<MerchantDashboard | null>(null);
  readonly categories = signal<Category[]>([]);
  readonly loading    = signal(true);
  readonly activeTab  = signal<Tab>('perfil');

  readonly saving     = signal(false);
  readonly saved      = signal(false);
  readonly savingCats = signal(false);
  readonly savedCats  = signal(false);

  readonly uploadingPhoto   = signal(false);
  readonly photoPreview     = signal<string | null>(null);
  readonly addressConfirmed = signal(false);

  readonly mpConnecting          = signal(false);
  readonly mpDisconnecting       = signal(false);
  readonly mpError               = signal<string | null>(null);
  readonly showDisconnectConfirm = signal(false);

  form = { businessName: '', address: '', contactPhone: '' };
  selectedCategoryIds = new Set<number>();
  private latitude  = 0;
  private longitude = 0;

  ngOnInit(): void {
    this.catalog.getCategories().subscribe(cats => this.categories.set(cats));
    this.merchant.getDashboard().subscribe({ next: d => this.dashboard.set(d), error: () => {} });

    this.merchant.getProfile().subscribe({
      next: p => {
        this.profile.set(p);
        this.form = { businessName: p.businessName, address: p.address, contactPhone: p.contactPhone };
        this.selectedCategoryIds = new Set(p.categories.map(c => c.id));
        this.latitude  = p.latitude;
        this.longitude = p.longitude;
        this.addressConfirmed.set(p.latitude !== 0 && p.longitude !== 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  setTab(tab: Tab): void { this.activeTab.set(tab); }

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
    const input = this.addressInputEl;
    if (!input) return;
    const autocomplete = new (window as any).google.maps.places.Autocomplete(input, {
      types: ['address'],
      componentRestrictions: { country: 'ar' }
    });
    input.addEventListener('input', () => {
      if (this.addressConfirmed()) {
        this.addressConfirmed.set(false);
        this.latitude = 0;
        this.longitude = 0;
      }
    });
    autocomplete.addListener('place_changed', () => {
      const place = autocomplete.getPlace();
      if (!place.geometry?.location) return;
      this.form.address = place.formatted_address ?? '';
      this.latitude     = place.geometry.location.lat();
      this.longitude    = place.geometry.location.lng();
      this.addressConfirmed.set(true);
    });
  }

  toggleCategory(id: number): void {
    if (this.selectedCategoryIds.has(id)) this.selectedCategoryIds.delete(id);
    else this.selectedCategoryIds.add(id);
  }

  isSelected(id: number): boolean { return this.selectedCategoryIds.has(id); }

  saveProfileData(): void {
    this.saving.set(true);
    this.merchant.updateProfile(this.buildPayload()).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.saving.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 2500);
      },
      error: () => this.saving.set(false)
    });
  }

  saveCategories(): void {
    this.savingCats.set(true);
    this.merchant.updateProfile(this.buildPayload()).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.savingCats.set(false);
        this.savedCats.set(true);
        setTimeout(() => this.savedCats.set(false), 2500);
      },
      error: () => this.savingCats.set(false)
    });
  }

  private buildPayload(): UpdateMerchantProfilePayload {
    return {
      businessName: this.form.businessName.trim(),
      address:      this.form.address.trim(),
      contactPhone: this.form.contactPhone.trim(),
      latitude:     this.latitude,
      longitude:    this.longitude,
      categoryIds:  [...this.selectedCategoryIds]
    };
  }

  triggerFileInput(): void { this.fileInput.nativeElement.click(); }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => this.photoPreview.set(reader.result as string);
    reader.readAsDataURL(file);
    this.uploadingPhoto.set(true);
    this.merchant.uploadPhoto(file).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.photoPreview.set(null);
        this.uploadingPhoto.set(false);
      },
      error: () => {
        this.photoPreview.set(null);
        this.uploadingPhoto.set(false);
      }
    });
  }

  connectMp(): void {
    this.mpConnecting.set(true);
    this.mpError.set(null);
    this.mpService.getAuthUrl().subscribe({
      next: ({ authUrl }) => { window.location.href = authUrl; },
      error: () => {
        this.mpConnecting.set(false);
        this.mpError.set('No se pudo iniciar la conexión con Mercado Pago. Intentá de nuevo.');
      }
    });
  }

  requestDisconnect(): void  { this.mpError.set(null); this.showDisconnectConfirm.set(true); }
  cancelDisconnect(): void   { this.showDisconnectConfirm.set(false); }

  confirmDisconnect(): void {
    this.showDisconnectConfirm.set(false);
    this.mpDisconnecting.set(true);
    this.mpError.set(null);
    this.mpService.disconnect().subscribe({
      next: () => {
        this.mpDisconnecting.set(false);
        this.merchant.getProfile().subscribe(p => this.profile.set(p));
      },
      error: () => {
        this.mpDisconnecting.set(false);
        this.mpError.set('No se pudo desconectar la cuenta. Intentá de nuevo.');
      }
    });
  }

  logout(): void { this.auth.logout(); }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }

  get activePacks(): number { return this.dashboard()?.activePackCount ?? 0; }
  get avgRating():   number { return this.dashboard()?.averageRating    ?? 0; }
  get totalSales():  number { return this.dashboard()?.totalSales       ?? 0; }
}
