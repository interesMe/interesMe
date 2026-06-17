export interface DirectConversationParticipant {
  userId: string;
  displayName: string;
  joinedAt: string;
}

export interface DirectConversation {
  id: string;
  updatedAt: string;
  participantsCount: number;
  lastMessagePreview: string | null;
  participants: DirectConversationParticipant[];
}
