import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { AuthService } from '../../../core/services/auth.service';
import { MerchantProfile, UpdateMerchantProfilePayload } from '../../../core/models/merchant.models';
import { Category } from '../../../core/models/catalog.models';
import { LucideStore, LucideMapPin, LucideSave, LucideLogOut, LucideLeaf, LucideCheck, LucideCamera } from '@lucide/angular';

@Component({
  selector: 'app-merchant-profile',
  standalone: true,
  imports: [FormsModule, LucideStore, LucideMapPin, LucideSave, LucideLogOut, LucideLeaf, LucideCheck, LucideCamera],
  templateUrl: './merchant-profile.component.html'
})
export class MerchantProfileComponent implements OnInit {
  private readonly merchant = inject(MerchantService);
  private readonly catalog  = inject(CatalogService);
  private readonly auth     = inject(AuthService);

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  readonly profile        = signal<MerchantProfile | null>(null);
  readonly categories     = signal<Category[]>([]);
  readonly loading        = signal(true);
  readonly saving         = signal(false);
  readonly saved          = signal(false);
  readonly uploadingPhoto = signal(false);
  readonly photoPreview   = signal<string | null>(null);

  readonly geoLoading = signal(false);
  readonly geoMsg     = signal<string | null>(null);

  form = { businessName: '', address: '', contactPhone: '' };
  selectedCategoryIds = new Set<number>();
  private latitude = 0;
  private longitude = 0;

  ngOnInit(): void {
    this.catalog.getCategories().subscribe(cats => this.categories.set(cats));

    this.merchant.getProfile().subscribe({
      next: p => {
        this.profile.set(p);
        this.form = { businessName: p.businessName, address: p.address, contactPhone: p.contactPhone };
        this.selectedCategoryIds = new Set(p.categories.map(c => c.id));
        this.latitude = p.latitude;
        this.longitude = p.longitude;
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  toggleCategory(id: number): void {
    if (this.selectedCategoryIds.has(id)) this.selectedCategoryIds.delete(id);
    else this.selectedCategoryIds.add(id);
  }

  isSelected(id: number): boolean {
    return this.selectedCategoryIds.has(id);
  }

  detectLocation(): void {
    if (!navigator.geolocation) { this.geoMsg.set('Tu navegador no soporta geolocalización.'); return; }
    this.geoLoading.set(true);
    this.geoMsg.set(null);
    navigator.geolocation.getCurrentPosition(
      pos => {
        this.latitude = pos.coords.latitude;
        this.longitude = pos.coords.longitude;
        this.geoLoading.set(false);
        this.geoMsg.set('Ubicación actualizada. Guardá los cambios para aplicar.');
      },
      () => { this.geoLoading.set(false); this.geoMsg.set('No se pudo obtener la ubicación.'); }
    );
  }

  saveProfile(): void {
    this.saving.set(true);
    const payload: UpdateMerchantProfilePayload = {
      businessName: this.form.businessName.trim(),
      address: this.form.address.trim(),
      contactPhone: this.form.contactPhone.trim(),
      latitude: this.latitude,
      longitude: this.longitude,
      categoryIds: [...this.selectedCategoryIds]
    };
    this.merchant.updateProfile(payload).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.saving.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 2500);
      },
      error: () => this.saving.set(false)
    });
  }

  triggerFileInput(): void {
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    // preview local inmediato antes de subir
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

  logout(): void { this.auth.logout(); }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }
}
