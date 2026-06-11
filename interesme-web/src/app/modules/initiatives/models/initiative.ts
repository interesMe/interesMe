import { InitiativeGoalType, InitiativeStatus, InitiativeVisibility } from './initiative.enums';
import { InitiativeInterest } from './initiative-interest';
import { InitiativeRole } from './initiative-role';

export interface Initiative {
  id: string;
  ownerUserId: string;
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
}
