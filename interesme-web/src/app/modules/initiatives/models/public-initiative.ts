import { InitiativeGoalType, InitiativeStatus, InitiativeVisibility } from './initiative.enums';
import { InitiativeInterest } from './initiative-interest';
import { InitiativeRole } from './initiative-role';

export interface PublicInitiative {
  id: string;
  slug: string;
  title: string;
  shortDescription: string;
  goalType: InitiativeGoalType;
  status: InitiativeStatus;
  visibility: InitiativeVisibility;
  university: string | null;
  teamSize: number | null;
  ownerDisplayName: string | null;
  createdAt: string;
  interests: InitiativeInterest[];
  roles: InitiativeRole[];
}
