import { Component, inject, signal, AfterViewInit, ElementRef, ViewChild, NgZone } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideLeaf, LucideUser, LucideStore, LucideTriangleAlert } from '@lucide/angular';
import { AuthService } from '../../../core/services/auth.service';
import { AuthLeftPanelComponent } from '../../../layouts/auth-layout/auth-left-panel.component';

declare const google: any;

const GOOGLE_CLIENT_ID = '1057032626071-0rv6obmuvhnh2457rj1mqfvc8kd73a92.apps.googleusercontent.com';

@Component({
  selector: 'app-role-picker',
  standalone: true,
  imports: [RouterLink, LucideLeaf, LucideUser, LucideStore, LucideTriangleAlert, AuthLeftPanelComponent],
  templateUrl: './role-picker.component.html'
})
export class RolePickerComponent implements AfterViewInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly ngZone = inject(NgZone);

  @ViewChild('googleBtn') googleBtn!: ElementRef;

  readonly googleLoading = signal(false);
  readonly googleError = signal('');

  ngAfterViewInit(): void {
    if (typeof google !== 'undefined' && google.accounts) {
      this.initGoogle();
    } else {
      window.addEventListener('load', () => {
        this.ngZone.run(() => {
          if (typeof google !== 'undefined') this.initGoogle();
        });
      }, { once: true });
    }
  }

  private initGoogle(): void {
    google.accounts.id.initialize({
      client_id: GOOGLE_CLIENT_ID,
      callback: (res: { credential: string }) =>
        this.ngZone.run(() => this.handleGoogleCallback(res))
    });
    google.accounts.id.renderButton(this.googleBtn.nativeElement, {
      type: 'standard',
      shape: 'rectangular',
      theme: 'outline',
      text: 'continue_with',
      size: 'large',
      locale: 'es',
      width: 368
    });
  }

  private handleGoogleCallback(response: { credential: string }): void {
    this.googleLoading.set(true);
    this.googleError.set('');
    this.authService.loginWithGoogle(response.credential).subscribe({
      next: () => {
        this.googleLoading.set(false);
        this.router.navigate(['/home']);
      },
      error: (err: HttpErrorResponse) => {
        this.googleLoading.set(false);
        this.googleError.set(err.error?.detail ?? 'Error al registrarse con Google. Intentá de nuevo.');
      }
    });
  }
}
