import { Directive, ElementRef, inject, input, effect } from '@angular/core';

/**
 * Drop-in replacement for `[src]` on product/merchant photos backed by our own MinIO
 * storage (URLs containing `/storage/`).
 *
 * Those URLs can point at the app's ngrok tunnel, whose free-tier anti-abuse
 * interstitial (ERR_NGROK_6024) blocks any browser that hasn't first visited that
 * exact host directly — which a plain `<img src>` request can never do, since it
 * can't set the `ngrok-skip-browser-warning` bypass header. This directive fetches
 * the image itself (with that header) and renders it as a blob URL instead.
 *
 * External URLs (e.g. seeded Wikimedia photos) are left as a normal `src` so they
 * keep native browser caching — only our own `/storage/` uploads pay the fetch cost.
 */
@Directive({
  selector: 'img[safeImg]',
  standalone: true
})
export class SafeImgDirective {
  private readonly el = inject(ElementRef<HTMLImageElement>);
  private objectUrl: string | null = null;

  readonly safeImg = input<string | null | undefined>(null);

  constructor() {
    effect(onCleanup => {
      const url = this.safeImg();
      this.revokePrevious();

      if (!url) {
        this.el.nativeElement.removeAttribute('src');
        return;
      }

      if (!url.includes('/storage/')) {
        this.el.nativeElement.src = url;
        return;
      }

      let cancelled = false;

      fetch(url, {
        headers: { 'ngrok-skip-browser-warning': 'true' },
        credentials: 'omit'
      })
        .then(res => (res.ok ? res.blob() : Promise.reject(res.status)))
        .then(blob => {
          if (cancelled) return;
          this.objectUrl = URL.createObjectURL(blob);
          this.el.nativeElement.src = this.objectUrl;
        })
        .catch(() => {
          // Leave the <img> without a src — nothing more we can do client-side.
        });

      onCleanup(() => {
        cancelled = true;
        this.revokePrevious();
      });
    });
  }

  private revokePrevious(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
    }
  }
}
