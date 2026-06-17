export type ChatConversationType = 1 | 2 | 3;

export interface ChatMessage {
  id: string;
  conversationType: ChatConversationType;
  directConversationId: string | null;
  groupChatId: string | null;
  channelId: string | null;
  senderUserId: string;
  senderDisplayName: string | null;
  text: string;
  createdAt: string;
  editedAt: string | null;
  isDeleted: boolean;
}

export interface SendMessageRequest {
  text: string;
}
