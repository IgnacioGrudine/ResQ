import { Component, ElementRef, computed, inject, input, signal } from '@angular/core';

/**
 * A single option inside a `<resq-select>`. Projected as content, not declared in
 * resq-select's own template — it reports clicks by dispatching a bubbling native
 * `resq-option-select` CustomEvent, so the parent select needs no direct reference
 * to this class (avoids a circular import between the two components).
 */
@Component({
  selector: 'resq-option',
  standalone: true,
  template: `<ng-content />`,
  host: {
    '[class]': 'hostClasses()',
    '(click)': 'onClick()',
  },
})
export class ResqOptionComponent {
  value = input.required<unknown>();

  private readonly el = inject(ElementRef<HTMLElement>);

  /** Set imperatively by the parent ResqSelectComponent whenever the selected value changes. */
  readonly isSelected = signal(false);

  readonly hostClasses = computed(() =>
    'block px-3.5 py-2.5 text-sm cursor-pointer transition-colors duration-150 ' +
    (this.isSelected()
      ? 'bg-fern/15 text-hunter font-semibold'
      : 'text-gray-700 hover:bg-lime/50 hover:text-hunter')
  );

  get label(): string {
    return this.el.nativeElement.textContent?.trim() ?? '';
  }

  onClick(): void {
    this.el.nativeElement.dispatchEvent(new CustomEvent('resq-option-select', {
      bubbles: true,
      detail: { value: this.value(), label: this.label }
    }));
  }
}
