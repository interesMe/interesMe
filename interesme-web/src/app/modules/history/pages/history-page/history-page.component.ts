import { Component, OnInit, computed, inject, signal } from '@angular/core';

import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { HistoryEvent, HistoryEventType, HistorySummary } from '../../models/history-event.model';
import { HistoryService } from '../../services/history.service';

type HistoryFilter = 'all' | 'initiatives' | 'interests' | 'events' | 'communities' | 'achievements' | 'posts';

type HistoryGroup = {
  key: string;
  label: string;
  events: HistoryEvent[];
};

@Component({
  selector: 'app-history-page',
  standalone: true,
  templateUrl: './history-page.component.html',
  styleUrl: './history-page.component.scss',
})
export class HistoryPageComponent implements OnInit {
  private readonly historyService = inject(HistoryService);
  private readonly i18n = inject(I18nService);

  readonly selectedFilter = signal<HistoryFilter>('all');
  readonly events = this.historyService.allEvents;
  readonly isLoading = this.historyService.isLoading;
  readonly errorMessage = this.historyService.errorMessage;

  readonly filters: HistoryFilter[] = ['all', 'initiatives', 'interests', 'events', 'communities', 'achievements', 'posts'];

  readonly summary = computed<HistorySummary>(() => {
    const events = this.events();

    return {
      initiativesCreated: events.filter((event) => event.type === 'created_initiative').length,
      initiativesJoined: events.filter((event) => event.type === 'joined_initiative').length,
      eventsOrganized: events.filter((event) => event.type === 'organized_event').length,
      interestsAdded: events.filter((event) => event.type === 'added_interest').length,
      communitiesJoined: events.filter((event) => event.type === 'joined_community').length,
      achievements: events.filter((event) => event.type === 'achievement_unlocked').length,
    };
  });

  readonly summaryCards = computed(() => [
    { label: this.t('historyPage.summary.created'), value: this.summary().initiativesCreated },
    { label: this.t('historyPage.summary.joined'), value: this.summary().initiativesJoined },
    { label: this.t('historyPage.summary.events'), value: this.summary().eventsOrganized },
    { label: this.t('historyPage.summary.interests'), value: this.summary().interestsAdded },
    { label: this.t('historyPage.summary.communities'), value: this.summary().communitiesJoined },
    { label: this.t('historyPage.summary.achievements'), value: this.summary().achievements },
  ]);

  readonly filteredEvents = computed(() => {
    const filter = this.selectedFilter();
    const events = [...this.events()].sort((first, second) => Date.parse(second.date) - Date.parse(first.date));

    if (filter === 'all') {
      return events;
    }

    return events.filter((event) => this.matchesFilter(event.type, filter));
  });

  readonly groupedEvents = computed<HistoryGroup[]>(() => {
    const groups = new Map<string, HistoryEvent[]>();

    for (const event of this.filteredEvents()) {
      const date = new Date(event.date);
      const key = `${date.getUTCFullYear()}-${date.getUTCMonth()}`;
      groups.set(key, [...(groups.get(key) ?? []), event]);
    }

    return Array.from(groups.entries()).map(([key, events]) => ({
      key,
      label: this.monthLabel(events[0].date),
      events,
    }));
  });

  ngOnInit(): void {
    this.historyService.loadMyHistory();
  }

  selectFilter(filter: HistoryFilter): void {
    this.selectedFilter.set(filter);
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  filterLabel(filter: HistoryFilter): string {
    return this.t(`historyPage.filters.${filter}` as TranslationKey);
  }

  eventDate(value: string): string {
    const date = new Date(value);
    const day = date.getUTCDate().toString().padStart(2, '0');
    const month = (date.getUTCMonth() + 1).toString().padStart(2, '0');

    return `${day}.${month}.${date.getUTCFullYear()}`;
  }

  private matchesFilter(type: HistoryEventType, filter: HistoryFilter): boolean {
    switch (filter) {
      case 'initiatives':
        return ['created_initiative', 'joined_initiative', 'completed_initiative', 'received_invitation'].includes(type);
      case 'interests':
        return type === 'added_interest';
      case 'events':
        return type === 'organized_event';
      case 'communities':
        return type === 'joined_community';
      case 'achievements':
        return type === 'achievement_unlocked';
      case 'posts':
        return type === 'created_post';
      default:
        return true;
    }
  }

  private monthLabel(value: string): string {
    const date = new Date(value);
    const monthKeys = [
      'historyPage.month.january',
      'historyPage.month.february',
      'historyPage.month.march',
      'historyPage.month.april',
      'historyPage.month.may',
      'historyPage.month.june',
      'historyPage.month.july',
      'historyPage.month.august',
      'historyPage.month.september',
      'historyPage.month.october',
      'historyPage.month.november',
      'historyPage.month.december',
    ] as const;

    return `${this.t(monthKeys[date.getUTCMonth()])} ${date.getUTCFullYear()}`;
  }
}
