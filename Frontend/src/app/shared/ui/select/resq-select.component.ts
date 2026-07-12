import {
  AfterContentInit, Component, ContentChildren, QueryList, computed, forwardRef, input, signal
} from '@angular/core';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { LucideChevronDown } from '@lucide/angular';
import { ResqOptionComponent } from './resq-option.component';

export type ResqSelectVariant = 'light' | 'dark';

/**
 * Custom dropdown that replaces native `<select>` so its option list can be styled
 * (native `<option>` hover/selected colors aren't reliably stylable across browsers).
 * Drop-in replacement for `[(ngModel)]` — implements ControlValueAccessor exactly
 * like ResqInputComponent. Usage:
 * ```html
 * <resq-select [(ngModel)]="foo">
 *   <resq-option [value]="1">Uno</resq-option>
 *   <resq-option [value]="2">Dos</resq-option>
 * </resq-select>
 * ```
 */
@Component({
  selector: 'resq-select',
  standalone: true,
  imports: [LucideChevronDown],
  templateUrl: './resq-select.component.html',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ResqSelectComponent),
      multi: true
    }
  ]
})
export class ResqSelectComponent implements ControlValueAccessor, AfterContentInit {
  placeholder = input<string>('Seleccionar');
  variant = input<ResqSelectVariant>('light');

  @ContentChildren(ResqOptionComponent) private optionsList!: QueryList<ResqOptionComponent>;

  readonly open = signal(false);
  readonly selectedLabel = signal('');

  isDisabled = false;
  private value: unknown = null;
  private onChange: (value: unknown) => void = () => {};
  private onTouched: () => void = () => {};

  readonly triggerClasses = computed(() =>
    this.variant() === 'dark'
      ? 'bg-white/10 border-white/20 text-white focus:ring-lime/50 focus:border-lime/50 hover:border-white/30'
      : 'bg-white border-gray-200 text-gray-700 focus:ring-fern focus:border-fern hover:border-gray-300'
  );

  readonly placeholderClasses = computed(() =>
    this.variant() === 'dark' ? 'text-white/50' : 'text-gray-400'
  );

  ngAfterContentInit(): void {
    this.syncSelectedState();
    // The option list can change shape after init (e.g. an @for bound to an
    // HTTP-fetched signal) — re-sync whenever that happens.
    this.optionsList.changes.subscribe(() => this.syncSelectedState());
  }

  private syncSelectedState(): void {
    let matched = '';
    this.optionsList.forEach(o => {
      const isMatch = o.value() === this.value;
      o.isSelected.set(isMatch);
      if (isMatch) matched = o.label;
    });
    this.selectedLabel.set(matched);
  }

  toggle(): void {
    if (this.isDisabled) return;
    this.open.update(v => !v);
  }

  close(): void {
    if (!this.open()) return;
    this.open.set(false);
    this.onTouched();
  }

  onOptionSelect(event: Event): void {
    const { value, label } = (event as CustomEvent<{ value: unknown; label: string }>).detail;
    this.value = value;
    this.selectedLabel.set(label);
    this.optionsList.forEach(o => o.isSelected.set(o.value() === value));
    this.onChange(value);
    this.close();
  }

  writeValue(value: unknown): void {
    this.value = value;
    if (this.optionsList) this.syncSelectedState();
  }

  registerOnChange(fn: (value: unknown) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }
}
