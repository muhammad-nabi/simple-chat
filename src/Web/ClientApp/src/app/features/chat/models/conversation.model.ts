export interface ParticipantInfo {
  userId: string;
  displayName: string;
}

export interface Conversation {
  id: number;
  type: string;
  name: string | null;
  lastMessagePreview: string | null;
  lastMessageAt: string | null;
  otherParticipants: ParticipantInfo[];
  unreadCount: number;
}
