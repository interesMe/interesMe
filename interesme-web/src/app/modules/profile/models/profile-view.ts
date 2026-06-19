import { InitiativeGoalType, InitiativeStatus, InitiativeVisibility } from '../../initiatives/models';

export interface ProfileViewResponse {
  basicProfile: BasicProfile;
  stats: ProfileStats;
  viewerRelation: ViewerRelation;
  interests: ProfileInterestCategory[];
  createdInitiatives: ProfileInitiativePreview[];
  joinedInitiatives: ProfileInitiativePreview[];
  recentHistory: ProfileHistoryPreview[];
  recentPosts: ProfilePostPreview[];
}

export interface BasicProfile {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  city: string | null;
  bio: string | null;
  activityStatus: ProfileActivityStatus;
  profileStatus: ProfileStatus;
  socialLinks: ProfileSocialLink[];
  joinedAt: string;
}

export type ProfileActivityStatus = 'online' | 'recently_active' | 'offline' | 'unknown';

export type ProfileStatus = 'active_member' | 'new_member' | 'community_builder' | 'initiative_creator';

export interface ProfileSocialLink {
  type: string;
  label: string;
  url: string;
}

export interface ProfileStats {
  interestsCount: number;
  sharedInterestsCount: number;
  createdInitiativesCount: number;
  joinedInitiativesCount: number;
  followersCount: number;
  followingCount: number;
  historyEventsCount: number;
  postsCount: number;
}

export interface ViewerRelation {
  isFollowing: boolean;
  canFollow: boolean;
  canMessage: boolean;
}

export interface ProfileInterestCategory {
  id: string;
  name: string;
  slug: string;
  interests: ProfileInterest[];
}

export interface ProfileInterest {
  id: string;
  name: string;
  slug: string;
}

export interface ProfileInitiativePreview {
  id: string;
  slug: string;
  title: string;
  shortDescription: string;
  goalType: InitiativeGoalType;
  status: InitiativeStatus;
  visibility: InitiativeVisibility;
  createdAt: string;
}

export interface ProfileHistoryPreview {
  id: string;
  type: string;
  title: string;
  description: string | null;
  targetType: string | null;
  targetId: string | null;
  targetName: string | null;
  metadataJson: string | null;
  occurredAt: string;
}

export interface ProfilePostPreview {
  id: string;
  title: string;
  body: string;
  type: string;
  interestPath: string | null;
  imageUrls: string[];
  createdAt: string;
}
