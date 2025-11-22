import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslationService, Language } from '@core/services/translation.service';
import { TranslatePipe } from '@shared/pipes/translate.pipe';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <div class="language-switcher">
      <button
        class="lang-button"
        [class.active]="currentLanguage() === 'en'"
        (click)="switchLanguage('en')"
        type="button"
        title="English">
        EN
      </button>
      <button
        class="lang-button"
        [class.active]="currentLanguage() === 'ar'"
        (click)="switchLanguage('ar')"
        type="button"
        title="العربية">
        AR
      </button>
    </div>
  `,
  styles: [`
    .language-switcher {
      display: flex;
      gap: 0.25rem;
      align-items: center;
      background-color: #f5f5f5;
      border-radius: 4px;
      padding: 0.25rem;
    }

    .lang-button {
      padding: 0.375rem 0.75rem;
      border: none;
      background-color: transparent;
      color: #666;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      border-radius: 3px;
      transition: all 0.2s ease;

      &:hover {
        background-color: #e0e0e0;
        color: #333;
      }

      &.active {
        background-color: #007bff;
        color: white;

        &:hover {
          background-color: #0056b3;
        }
      }

      &:focus {
        outline: none;
        box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
      }
    }
  `]
})
export class LanguageSwitcherComponent {
  currentLanguage = computed(() => this.translationService.currentLanguage());

  constructor(private translationService: TranslationService) {}

  switchLanguage(lang: Language): void {
    this.translationService.setLanguage(lang);
  }
}
