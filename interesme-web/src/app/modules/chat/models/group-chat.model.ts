export type ChatParticipantRole = 1 | 2 | 3;

export interface GroupChatParticipant {
  userId: string;
  displayName: string;
  role: ChatParticipantRole;
  joinedAt: string;
}

export interface GroupChat {
  id: string;
  initiativeId: string | null;
  title: string;
  updatedAt: string;
  participantsCount: number;
  lastMessagePreview: string | null;
  participants: GroupChatParticipant[];
}
