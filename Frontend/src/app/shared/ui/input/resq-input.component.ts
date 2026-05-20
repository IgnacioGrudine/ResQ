import { Component, input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';

export type InputType = 'text' | 'email' | 'password' | 'number' | 'tel';

@Component({
  selector: 'resq-input',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './resq-input.component.html',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ResqInputComponent),
      multi: true
    }
  ]
})
export class ResqInputComponent implements ControlValueAccessor {
  label = input<string>('');
  type = input<InputType>('text');
  placeholder = input<string>('');
  errorMessage = input<string>('');
  hint = input<string>('');
  required = input<boolean>(false);

  value = '';
  isDisabled = false;
  showPassword = false;

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  get effectiveType(): string {
    if (this.type() === 'password') {
      return this.showPassword ? 'text' : 'password';
    }
    return this.type();
  }

  get inputClasses(): string {
    const base =
      'w-full px-4 py-2.5 rounded-lg border bg-white text-evergreen placeholder-gray-400 ' +
      'focus:outline-none focus:ring-2 transition-colors duration-200 text-sm';
    const error = this.errorMessage()
      ? 'border-red-400 focus:ring-red-300'
      : 'border-gray-300 focus:ring-fern focus:border-fern';
    const disabled = this.isDisabled ? 'opacity-50 cursor-not-allowed bg-gray-50' : '';
    return [base, error, disabled].filter(Boolean).join(' ');
  }

  writeValue(value: string): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  onInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.value = val;
    this.onChange(val);
  }

  onBlur(): void {
    this.onTouched();
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }
}
