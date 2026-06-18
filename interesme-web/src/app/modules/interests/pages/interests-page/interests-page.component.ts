import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { Interest, InterestCategory } from '../../models/interest.model';
import { InterestsMockService } from '../../services/interests.mock';

@Component({
  selector: 'app-interests-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './interests-page.component.html',
  styleUrl: './interests-page.component.scss',
})
export class InterestsPageComponent {
  private readonly interestsService = inject(InterestsMockService);
  private readonly i18n = inject(I18nService);

  readonly searchTerm = signal('');
  readonly selectedCategoryId = signal<string | null>(null);
  readonly selectedInterestId = signal<string | null>(null);
  readonly followedInterestIds = signal<ReadonlySet<string>>(new Set<string>());

  readonly allCategories = this.interestsService.allCategories;
  readonly initiativesPath = `/${APP_ROUTES.initiatives}`;
  readonly createInitiativePath = `/${APP_ROUTES.initiativeCreate}`;

  readonly filteredCategories = computed(() => {
    const query = this.searchTerm().trim().toLowerCase();
    const categories = this.allCategories();

    if (!query) {
      return categories;
    }

    return categories.filter((category) => this.matchesCategoryQuery(category, query));
  });

  readonly selectedCategory = computed(() => {
    const selectedId = this.selectedCategoryId();

    if (!selectedId) {
      return null;
    }

    return this.allCategories().find((category) => category.id === selectedId) ?? null;
  });

  readonly selectedInterest = computed(() => {
    const selectedId = this.selectedInterestId();

    if (!selectedId) {
      return null;
    }

    return this.selectedCategory()?.interests.find((interest) => interest.id === selectedId) ?? null;
  });

  readonly panelCategory = computed(() => {
    return this.selectedInterest()
      ? this.selectedCategory()
      : this.selectedCategory();
  });

  onSearch(value: string): void {
    this.searchTerm.set(value);

    const selected = this.selectedCategory();
    if (selected && this.filteredCategories().some((category) => category.id === selected.id)) {
      return;
    }

    this.selectedCategoryId.set(null);
    this.selectedInterestId.set(null);
  }

  selectCategory(category: InterestCategory): void {
    this.selectedCategoryId.set(category.id);
    this.selectedInterestId.set(null);
  }

  selectInterest(interest: Interest): void {
    this.selectedCategoryId.set(interest.categoryId);
    this.selectedInterestId.set(interest.id);
  }

  toggleFollow(interest: Interest): void {
    this.followedInterestIds.update((current) => {
      const next = new Set(current);

      if (next.has(interest.id)) {
        next.delete(interest.id);
      } else {
        next.add(interest.id);
      }

      return next;
    });
  }

  isCategorySelected(category: InterestCategory): boolean {
    return this.panelCategory()?.id === category.id && !this.selectedInterest();
  }

  isInterestSelected(interest: Interest): boolean {
    return this.selectedInterest()?.id === interest.id;
  }

  isFollowed(interest: Interest): boolean {
    return this.followedInterestIds().has(interest.id);
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  categoryDescription(category: InterestCategory): string {
    return this.t(`interestsPage.categories.${category.id}.description` as TranslationKey);
  }

  private matchesCategoryQuery(category: InterestCategory, query: string): boolean {
    const searchableText = [
      category.name,
      category.description,
      ...category.interests.flatMap((interest) => [
        interest.name,
        interest.description,
        ...interest.subinterests.flatMap((subinterest) => [subinterest.name, subinterest.description ?? '']),
      ]),
    ]
      .join(' ')
      .toLowerCase();

    return searchableText.includes(query);
  }
}
