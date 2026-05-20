import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'resq-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './resq-button.component.html'
})
export class ResqButtonComponent {
  variant = input<ButtonVariant>('primary');
  size = input<ButtonSize>('md');
  disabled = input<boolean>(false);
  loading = input<boolean>(false);
  type = input<'button' | 'submit' | 'reset'>('button');
  fullWidth = input<boolean>(false);

  clicked = output<MouseEvent>();

  get classes(): string {
    const base =
      'inline-flex items-center justify-center gap-2 font-semibold rounded-lg transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed';

    const variants: Record<ButtonVariant, string> = {
      primary:
        'bg-hunter text-white hover:bg-evergreen focus:ring-hunter active:bg-evergreen',
      secondary:
        'bg-lime text-evergreen border border-palm hover:bg-palm hover:text-white focus:ring-palm',
      ghost:
        'bg-transparent text-hunter border border-hunter hover:bg-lime focus:ring-hunter',
      danger:
        'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500'
    };

    const sizes: Record<ButtonSize, string> = {
      sm: 'px-3 py-1.5 text-sm',
      md: 'px-5 py-2.5 text-base',
      lg: 'px-7 py-3.5 text-lg'
    };

    const width = this.fullWidth() ? 'w-full' : '';

    return [base, variants[this.variant()], sizes[this.size()], width]
      .filter(Boolean)
      .join(' ');
  }

  onClick(event: MouseEvent): void {
    if (!this.disabled() && !this.loading()) {
      this.clicked.emit(event);
    }
  }
}
