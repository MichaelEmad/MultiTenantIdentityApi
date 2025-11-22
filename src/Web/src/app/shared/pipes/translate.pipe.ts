import { Pipe, PipeTransform, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { TranslationService } from '@core/services/translation.service';
import { Subscription } from 'rxjs';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false
})
export class TranslatePipe implements PipeTransform, OnDestroy {
  private subscription: Subscription | null = null;

  constructor(
    private translationService: TranslationService,
    private cdr: ChangeDetectorRef
  ) {
    // Subscribe to language changes to trigger re-rendering
    this.subscription = this.translationService.currentLanguage$.subscribe(() => {
      this.cdr.markForCheck();
    });
  }

  transform(key: string, params?: { [key: string]: string | number }): string {
    return this.translationService.translate(key, params);
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
}
