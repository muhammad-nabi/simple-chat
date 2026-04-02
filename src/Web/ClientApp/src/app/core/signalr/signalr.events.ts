export const SignalREvents = {
  ReceiveMessage: 'ReceiveMessage',
  UserOnline: 'UserOnline',
  UserOffline: 'UserOffline',
} as const;

export interface MessagePayload {
  id: number;
  conversationId: number;
  senderId: string;
  senderDisplayName: string;
  content: string;
  sentAt: string;
  messageType: string;
}
