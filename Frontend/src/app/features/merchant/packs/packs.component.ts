import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';
import { MercadoPagoService } from '../../../core/services/mercadopago.service';
import { MerchantProduct, ProductPayload } from '../../../core/models/merchant.models';
import {
  LucidePackage,
  LucidePlus,
  LucidePencil,
  LucideTrash2,
  LucideX,
  LucideClock,
  LucideCamera,
  LucideImage,
  LucideLink
} from '@lucide/angular';

interface PackForm {
  name: string;
  description: string;
  imageUrl: string;
  productType: number;
  originalPrice: number | null;
  salePrice: number | null;
  stockQuantity: number | null;
  pickupTimeStart: string;
  pickupTimeEnd: string;
}

@Component({
  selector: 'app-merchant-packs',
  standalone: true,
  imports: [DecimalPipe, FormsModule, LucidePackage, LucidePlus, LucidePencil, LucideTrash2, LucideX, LucideClock, LucideCamera, LucideImage, LucideLink],
  templateUrl: './packs.component.html'
})
export class PacksComponent implements OnInit {
  private readonly merchant  = inject(MerchantService);
  private readonly mpService = inject(MercadoPagoService);

  @ViewChild('packImageInput') packImageInput!: ElementRef<HTMLInputElement>;

  readonly packs   = signal<MerchantProduct[]>([]);
  readonly loading = signal(true);

  /** Whether the merchant has a live Mercado Pago connection. Packs cannot be published without it. */
  readonly mpConnected    = signal(false);
  readonly mpConnecting   = signal(false);

  // Form modal state
  readonly formOpen    = signal(false);
  readonly saving      = signal(false);
  readonly formError   = signal<string | null>(null);
  readonly imagePreview = signal<string | null>(null);
  editingId: number | null = null;
  pendingImageFile: File | null = null;

  // Delete confirm state
  readonly deletingId = signal<number | null>(null);

  form: PackForm = this.emptyForm();

  ngOnInit(): void {
    this.load();
    this.merchant.getProfile().subscribe({
      next: p => this.mpConnected.set(p.mpConnectionStatus === 'Connected')
    });
  }

  connectMp(): void {
    this.mpConnecting.set(true);
    this.mpService.getAuthUrl().subscribe({
      next:  ({ authUrl }) => { window.location.href = authUrl; },
      error: () => this.mpConnecting.set(false)
    });
  }

  load(): void {
    this.loading.set(true);
    this.merchant.getProducts().subscribe({
      next:  p  => { this.packs.set(p); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  emptyForm(): PackForm {
    return {
      name: '', description: '', imageUrl: '',
      productType: 1,
      originalPrice: null, salePrice: null, stockQuantity: null,
      pickupTimeStart: '18:00', pickupTimeEnd: '21:00'
    };
  }

  openCreate(): void {
    // Hard rule: no MP connection → no publishing. The UI also hides this entry point.
    if (!this.mpConnected()) return;
    this.editingId = null;
    this.form = this.emptyForm();
    this.formError.set(null);
    this.pendingImageFile = null;
    this.imagePreview.set(null);
    this.formOpen.set(true);
  }

  openEdit(p: MerchantProduct): void {
    this.editingId = p.id;
    this.form = {
      name: p.name,
      description: p.description ?? '',
      imageUrl: p.imageUrl ?? '',
      productType: p.productType === 'ExplicitItem' ? 2 : 1,
      originalPrice: p.originalPrice,
      salePrice: p.salePrice,
      stockQuantity: p.stockQuantity,
      pickupTimeStart: p.pickupTimeStart.substring(0, 5),
      pickupTimeEnd: p.pickupTimeEnd.substring(0, 5)
    };
    this.formError.set(null);
    this.pendingImageFile = null;
    this.imagePreview.set(p.imageUrl ?? null);
    this.formOpen.set(true);
  }

  closeForm(): void {
    this.formOpen.set(false);
    this.pendingImageFile = null;
    this.imagePreview.set(null);
  }

  triggerImageInput(): void {
    this.packImageInput.nativeElement.click();
  }

  onImageSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.pendingImageFile = file;
    const reader = new FileReader();
    reader.onload = () => this.imagePreview.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  private validate(): string | null {
    const f = this.form;
    if (!f.name.trim()) return 'El nombre es obligatorio.';
    if (f.originalPrice == null || f.originalPrice <= 0) return 'El precio original debe ser mayor a 0.';
    if (f.salePrice == null || f.salePrice <= 0) return 'El precio de venta debe ser mayor a 0.';
    if (f.salePrice >= f.originalPrice) return 'El precio de venta debe ser menor al original.';
    if (f.stockQuantity == null || f.stockQuantity < 0) return 'El stock no puede ser negativo.';
    if (!f.pickupTimeStart || !f.pickupTimeEnd) return 'Indicá el horario de retiro.';
    if (f.pickupTimeStart >= f.pickupTimeEnd) return 'El horario de inicio debe ser anterior al de fin.';
    return null;
  }

  save(): void {
    const err = this.validate();
    if (err) { this.formError.set(err); return; }

    const f = this.form;
    const payload: ProductPayload = {
      name: f.name.trim(),
      description: f.description.trim() || undefined,
      imageUrl: f.imageUrl.trim() || undefined,
      productType: f.productType,
      originalPrice: f.originalPrice!,
      salePrice: f.salePrice!,
      stockQuantity: f.stockQuantity!,
      pickupTimeStart: this.withSeconds(f.pickupTimeStart),
      pickupTimeEnd: this.withSeconds(f.pickupTimeEnd)
    };

    this.saving.set(true);
    this.formError.set(null);

    const req = this.editingId == null
      ? this.merchant.createProduct(payload)
      : this.merchant.updateProduct(this.editingId, payload);

    req.subscribe({
      next: (saved) => {
        if (this.pendingImageFile) {
          this.merchant.uploadPackImage(saved.id, this.pendingImageFile).subscribe({
            next:  () => this.finishSave(),
            error: () => this.finishSave()
          });
        } else {
          this.finishSave();
        }
      },
      error: () => { this.saving.set(false); this.formError.set('No se pudo guardar el pack. Intentá de nuevo.'); }
    });
  }

  private finishSave(): void {
    this.saving.set(false);
    this.formOpen.set(false);
    this.pendingImageFile = null;
    this.imagePreview.set(null);
    this.load();
  }

  toggle(p: MerchantProduct): void {
    // Activating requires MP; deactivating is always allowed. Mirrors the backend gate.
    if (!p.isActive && !this.mpConnected()) return;
    this.merchant.toggleProduct(p.id).subscribe({
      next: updated => this.packs.update(list => list.map(x => x.id === updated.id ? updated : x))
    });
  }

  askDelete(id: number): void { this.deletingId.set(id); }
  cancelDelete(): void { this.deletingId.set(null); }

  confirmDelete(id: number): void {
    this.merchant.deleteProduct(id).subscribe({
      next: () => { this.deletingId.set(null); this.packs.update(list => list.filter(x => x.id !== id)); }
    });
  }

  discountPercent(p: MerchantProduct): number {
    if (!p.originalPrice) return 0;
    return Math.round((1 - p.salePrice / p.originalPrice) * 100);
  }

  formatTime(t: string): string { return t.substring(0, 5); }

  // <input type="time"> yields "HH:mm"; normalize to "HH:mm:ss" for TimeOnly binding
  private withSeconds(t: string): string { return t.length === 5 ? `${t}:00` : t; }

  statusLabel(p: MerchantProduct): string {
    if (!p.isActive) return 'Inactivo';
    if (p.stockQuantity <= 0) return 'Sin stock';
    return 'Activo';
  }

  statusClasses(p: MerchantProduct): string {
    if (!p.isActive) return 'bg-gray-100 text-gray-500';
    if (p.stockQuantity <= 0) return 'bg-amber-50 text-amber-600 border border-amber-200';
    return 'bg-lime/60 text-hunter border border-fern/30';
  }
}
