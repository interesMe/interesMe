import { InitiativeGoalType, InitiativeStatus, InitiativeVisibility } from './initiative.enums';

export interface CreateInitiativeRequest {
  title: string;
  shortDescription: string;
  goalType: InitiativeGoalType;
  interestIds: string[];
  roles: string[];
  university?: string | null;
  teamSize?: number | null;
  status?: InitiativeStatus | null;
  visibility?: InitiativeVisibility | null;
}
