export type ChatConversationType = 1 | 2 | 3;

export interface ChatMessage {
  id: string;
  conversationType: ChatConversationType;
  directConversationId: string | null;
  groupChatId: string | null;
  channelId: string | null;
  senderUserId: string;
  senderDisplayName: string | null;
  text: string | null;
  attachments: ChatMessageAttachment[];
  createdAt: string;
  editedAt: string | null;
  isDeleted: boolean;
}

export interface ChatMessageAttachment {
  id: string;
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

export interface SendMessageRequest {
  text: string;
}
