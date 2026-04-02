export type CheckmarkState = 'sending' | 'sent';

export interface Message {
  id: number;
  conversationId: number;
  senderId: string;
  senderDisplayName: string;
  content: string;
  sentAt: string;
  messageType: string;
  checkmarkState?: CheckmarkState;
}

export interface MessageHistoryResponse {
  messages: Message[];
  hasMore: boolean;
  nextCursor: number | null;
}
