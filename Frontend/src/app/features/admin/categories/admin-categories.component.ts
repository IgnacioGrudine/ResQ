import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../../../core/services/catalog.service';
import { Category } from '../../../core/models/catalog.models';
import {
  LucideTags, LucidePlus, LucidePencil, LucideTrash2, LucideX
} from '@lucide/angular';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [FormsModule, LucideTags, LucidePlus, LucidePencil, LucideTrash2, LucideX],
  templateUrl: './admin-categories.component.html'
})
export class AdminCategoriesComponent implements OnInit {
  private readonly catalog = inject(CatalogService);

  readonly categories  = signal<Category[]>([]);
  readonly loading     = signal(true);
  readonly saving      = signal(false);
  readonly formOpen    = signal(false);
  readonly formError   = signal<string | null>(null);
  readonly deletingId  = signal<number | null>(null);
  readonly deleteError = signal<string | null>(null);

  editingId: number | null = null;
  name = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.catalog.getCategories().subscribe({
      next:  cats => { this.categories.set(cats); this.loading.set(false); },
      error: ()   => this.loading.set(false)
    });
  }

  openCreate(): void {
    this.editingId = null;
    this.name = '';
    this.formError.set(null);
    this.formOpen.set(true);
  }

  openEdit(cat: Category): void {
    this.editingId = cat.id;
    this.name = cat.name;
    this.formError.set(null);
    this.formOpen.set(true);
  }

  closeForm(): void { this.formOpen.set(false); }

  save(): void {
    const trimmed = this.name.trim();
    if (!trimmed) { this.formError.set('El nombre es obligatorio.'); return; }

    this.saving.set(true);
    this.formError.set(null);

    const req = this.editingId == null
      ? this.catalog.createCategory(trimmed)
      : this.catalog.updateCategory(this.editingId, trimmed);

    req.subscribe({
      next:  () => { this.saving.set(false); this.formOpen.set(false); this.load(); },
      error: () => { this.saving.set(false); this.formError.set('No se pudo guardar la categoría. Intentá de nuevo.'); }
    });
  }

  askDelete(id: number): void { this.deletingId.set(id); this.deleteError.set(null); }
  cancelDelete(): void { this.deletingId.set(null); this.deleteError.set(null); }

  confirmDelete(id: number): void {
    this.catalog.deleteCategory(id).subscribe({
      next:  () => { this.deletingId.set(null); this.categories.update(list => list.filter(c => c.id !== id)); },
      error: (err) => {
        const detail = err?.error?.detail ?? 'No se pudo eliminar la categoría.';
        this.deleteError.set(detail);
      }
    });
  }
}
