import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, finalize, map, of } from 'rxjs';

import { HISTORY_API_ENDPOINTS } from '../../../core/constants/api.constants';
import { HistoryEvent, HistoryEventResponse, HistoryResponse, HistoryTargetType } from '../models/history-event.model';
import { MOCK_HISTORY_EVENTS } from './history.mock';

@Injectable({ providedIn: 'root' })
export class HistoryService {
  private readonly http = inject(HttpClient);
  private readonly useMockData = false;
  private readonly events = signal<HistoryEvent[]>([]);
  private readonly loading = signal(false);
  private readonly error = signal(false);

  readonly allEvents = this.events.asReadonly();
  readonly isLoading = this.loading.asReadonly();
  readonly hasError = this.error.asReadonly();

  loadMyHistory(): void {
    this.loading.set(true);
    this.error.set(false);

    if (this.useMockData) {
      this.events.set(MOCK_HISTORY_EVENTS);
      this.loading.set(false);
      return;
    }

    this.http
      .get<HistoryResponse | null>(HISTORY_API_ENDPOINTS.myHistory)
      .pipe(
        map((response) => (response?.events ?? []).map((event) => this.mapEvent(event))),
        catchError(() => {
          this.error.set(true);
          this.events.set([]);
          return of([]);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((events) => {
        this.events.set(events);
      });
  }

  private mapEvent(event: HistoryEventResponse): HistoryEvent {
    return {
      id: event.id,
      userId: event.userId,
      type: event.type,
      title: event.title,
      description: event.description ?? undefined,
      date: event.date ?? event.occurredAt ?? event.createdAt ?? new Date().toISOString(),
      icon: event.icon ?? this.iconFor(event.type),
      accent: event.accent ?? this.accentFor(event.type),
      targetType: this.mapTargetType(event.targetType),
      targetId: event.targetId ?? undefined,
      targetName: event.targetName ?? undefined,
      metadata: this.parseMetadata(event.metadataJson),
    };
  }

  private parseMetadata(metadataJson?: string | null): HistoryEvent['metadata'] {
    if (!metadataJson) {
      return undefined;
    }

    try {
      const metadata = JSON.parse(metadataJson) as HistoryEvent['metadata'];
      return metadata && typeof metadata === 'object' ? metadata : undefined;
    } catch {
      return undefined;
    }
  }

  private mapTargetType(targetType?: string | null): HistoryTargetType | undefined {
    if (
      targetType === 'initiative' ||
      targetType === 'interest' ||
      targetType === 'post' ||
      targetType === 'community' ||
      targetType === 'event' ||
      targetType === 'achievement'
    ) {
      return targetType;
    }

    return undefined;
  }

  private iconFor(type: HistoryEvent['type']): string {
    switch (type) {
      case 'created_initiative':
        return 'rocket';
      case 'joined_initiative':
        return 'users';
      case 'completed_initiative':
        return 'check-circle';
      case 'added_interest':
        return 'sparkles';
      case 'joined_community':
        return 'network';
      case 'organized_event':
        return 'calendar';
      case 'created_post':
        return 'file-text';
      case 'achievement_unlocked':
        return 'trophy';
      case 'received_invitation':
        return 'mail';
      default:
        return 'clock';
    }
  }

  private accentFor(type: HistoryEvent['type']): string {
    switch (type) {
      case 'created_initiative':
        return '#14b8a6';
      case 'joined_initiative':
        return '#38bdf8';
      case 'completed_initiative':
        return '#22c55e';
      case 'added_interest':
        return '#2dd4bf';
      case 'joined_community':
        return '#818cf8';
      case 'organized_event':
        return '#f59e0b';
      case 'created_post':
        return '#fb7185';
      case 'achievement_unlocked':
        return '#facc15';
      case 'received_invitation':
        return '#a78bfa';
      default:
        return '#64748b';
    }
  }
}
