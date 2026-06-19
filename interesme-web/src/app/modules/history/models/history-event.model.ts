export type HistoryEventType =
  | 'created_initiative'
  | 'joined_initiative'
  | 'completed_initiative'
  | 'added_interest'
  | 'created_post'
  | 'organized_event'
  | 'joined_community'
  | 'achievement_unlocked'
  | 'received_invitation';

export type HistoryTargetType = 'initiative' | 'interest' | 'post' | 'community' | 'event' | 'achievement';

export interface HistoryEvent {
  id: string;
  userId: string;
  type: HistoryEventType;
  title: string;
  description?: string;
  date: string;
  icon: string;
  accent: string;
  targetType?: HistoryTargetType;
  targetId?: string;
  targetName?: string;
  metadata?: {
    category?: string;
    interestPath?: string;
    participantsCount?: number;
    result?: string;
    location?: string;
  };
}

export interface HistorySummary {
  initiativesCreated: number;
  initiativesJoined: number;
  eventsOrganized: number;
  interestsAdded: number;
  communitiesJoined: number;
  achievements: number;
}

export interface HistoryEventResponse {
  id: string;
  userId: string;
  type: HistoryEventType;
  title: string;
  description?: string | null;
  date?: string | null;
  icon?: string | null;
  accent?: string | null;
  targetType?: string | null;
  targetId?: string | null;
  targetName?: string | null;
  metadataJson?: string | null;
  occurredAt?: string | null;
  createdAt?: string | null;
}

export interface HistoryResponse {
  events: HistoryEventResponse[];
}
