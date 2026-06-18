import { Injectable, signal } from '@angular/core';

import { HistoryEvent } from '../models/history-event.model';
import { MOCK_HISTORY_EVENTS } from './history.mock';

@Injectable({ providedIn: 'root' })
export class HistoryService {
  private readonly events = signal<HistoryEvent[]>(MOCK_HISTORY_EVENTS);
  private readonly loading = signal(false);
  private readonly error = signal<string | null>(null);

  readonly allEvents = this.events.asReadonly();
  readonly isLoading = this.loading.asReadonly();
  readonly errorMessage = this.error.asReadonly();

  loadMyHistory(): void {
    this.loading.set(true);
    this.error.set(null);

    window.setTimeout(() => {
      this.events.set(MOCK_HISTORY_EVENTS);
      this.loading.set(false);
    }, 180);
  }
}
