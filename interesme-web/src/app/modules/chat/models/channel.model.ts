import { ChatParticipantRole } from './group-chat.model';

export interface ChannelMember {
  userId: string;
  displayName: string;
  role: ChatParticipantRole;
  joinedAt: string;
}

export interface Channel {
  id: string;
  initiativeId: string | null;
  title: string;
  updatedAt: string;
  membersCount: number;
  members: ChannelMember[];
}
