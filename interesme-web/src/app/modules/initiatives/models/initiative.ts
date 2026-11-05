import { InitiativeGoalType, InitiativeJoinRequestStatus, InitiativeStatus, InitiativeVisibility } from './initiative.enums';
import { InitiativeInterest } from './initiative-interest';
import { InitiativeRole } from './initiative-role';

export interface Initiative {
  id: string;
  ownerUserId: string;
  ownerDisplayName: string;
  slug: string;
  title: string;
  shortDescription: string;
  goalType: InitiativeGoalType;
  university: string | null;
  teamSize: number | null;
  status: InitiativeStatus;
  visibility: InitiativeVisibility;
  createdAt: string;
  updatedAt: string;
  interests: InitiativeInterest[];
  roles: InitiativeRole[];
  /** Accepted participants (owner excluded); filled by the details endpoint. */
  members: InitiativeMember[];
  memberCount: number;
  /** The current user's latest join request, if any. */
  myJoinRequest: ViewerJoinRequest | null;
}

export interface InitiativeMember {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  roleName: string | null;
  joinedAt: string;
}

export interface ViewerJoinRequest {
  id: string;
  status: InitiativeJoinRequestStatus;
  roleId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface InitiativeListQuery {
  search?: string;
  goalType?: number | null;
  limit?: number;
}
