import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { I18nService } from '../../../../core/i18n/i18n.service';
import { ProfileHistoryPreview } from '../../models';

@Component({
  selector: 'app-profile-history-timeline',
  standalone: true,
  templateUrl: './profile-history-timeline.html',
  styleUrl: './profile-history-timeline.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileHistoryTimelineComponent {
  private readonly i18n = inject(I18nService);

  readonly events = input.required<ProfileHistoryPreview[]>();
  readonly emptyText = input.required<string>();
  readonly locale = computed(() => (this.i18n.currentLocale() === 'uk' ? 'uk-UA' : 'en-US'));

  eventDate(value: string): string {
    return new Intl.DateTimeFormat(this.locale(), {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    }).format(new Date(value));
  }

  eventIcon(type: string): string {
    switch (type) {
      case 'created_initiative':
        return '↗';
      case 'joined_initiative':
        return '＋';
      case 'completed_initiative':
        return '✓';
      case 'added_interest':
        return '✦';
      case 'created_post':
        return '✎';
      case 'organized_event':
        return '◇';
      default:
        return '•';
    }
  }

  eventAccent(type: string): string {
    switch (type) {
      case 'created_initiative':
        return 'var(--im-primary)';
      case 'joined_initiative':
        return 'var(--im-explore)';
      case 'completed_initiative':
        return 'var(--im-learn)';
      case 'created_post':
        return 'var(--im-coral)';
      case 'organized_event':
        return 'var(--im-play)';
      default:
        return 'var(--im-primary-hover)';
    }
  }

  metadataSummary(metadataJson: string | null): string | null {
    if (!metadataJson) {
      return null;
    }

    try {
      const metadata = JSON.parse(metadataJson) as Record<string, unknown>;
      const values = Object.values(metadata)
        .filter((value): value is string | number => typeof value === 'string' || typeof value === 'number')
        .slice(0, 2);

      return values.length > 0 ? values.join(' · ') : null;
    } catch {
      return null;
    }
  }
}
