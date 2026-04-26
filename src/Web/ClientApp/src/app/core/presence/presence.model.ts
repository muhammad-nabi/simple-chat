export interface OnlineUser {
  userId: string;
  displayName: string;
  status: 'Online' | 'Away' | 'Offline';
}
