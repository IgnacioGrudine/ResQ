import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { ConsumerService } from '../../../core/services/consumer.service';
import { AuthService } from '../../../core/services/auth.service';
import { ConsumerProfile } from '../../../core/models/consumer.models';
import { LucideSave, LucideLeaf } from '@lucide/angular';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule, DecimalPipe, LucideSave, LucideLeaf],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  private readonly consumer = inject(ConsumerService);
  private readonly auth     = inject(AuthService);

  readonly profile  = signal<ConsumerProfile | null>(null);
  readonly loading  = signal(true);
  readonly saving   = signal(false);
  readonly saved    = signal(false);

  form = { firstName: '', lastName: '', phoneNumber: '' };

  ngOnInit(): void {
    this.consumer.getProfile().subscribe({
      next: p => {
        this.profile.set(p);
        this.form = { firstName: p.firstName, lastName: p.lastName, phoneNumber: p.phoneNumber ?? '' };
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  saveProfile(): void {
    this.saving.set(true);
    this.consumer.updateProfile(this.form).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.saving.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 2500);
      },
      error: () => this.saving.set(false)
    });
  }

  logout(): void { this.auth.logout(); }

  initials(p: ConsumerProfile): string {
    return [p.firstName[0], p.lastName[0]].join('').toUpperCase();
  }
}
