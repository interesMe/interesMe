import { InitiativeJoinRequestStatus } from './initiative.enums';

export interface InitiativeJoinRequest {
  id: string;
  initiativeId: string;
  userId: string;
  roleId: string | null;
  message: string | null;
  motivation: string | null;
  experience: string | null;
  contribution: string | null;
  availability: string | null;
  status: InitiativeJoinRequestStatus;
  createdAt: string;
  updatedAt: string;
}
