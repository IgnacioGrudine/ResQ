import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type CardVariant = 'default' | 'elevated' | 'outlined' | 'lime';

@Component({
  selector: 'resq-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './resq-card.component.html'
})
export class ResqCardComponent {
  variant = input<CardVariant>('default');
  padding = input<boolean>(true);
  hover = input<boolean>(false);

  get classes(): string {
    const base = 'rounded-2xl transition-all duration-200';

    const variants: Record<CardVariant, string> = {
      default: 'bg-white shadow-sm border border-gray-100',
      elevated: 'bg-white shadow-md border border-gray-100',
      outlined: 'bg-white border border-palm',
      lime: 'bg-lime border border-palm'
    };

    const hoverClass = this.hover()
      ? 'hover:shadow-lg hover:-translate-y-0.5 cursor-pointer'
      : '';

    const paddingClass = this.padding() ? 'p-6' : '';

    return [base, variants[this.variant()], hoverClass, paddingClass]
      .filter(Boolean)
      .join(' ');
  }
}
