export interface GetAuthStatusResponse {
  isAuthenticated: boolean;
  provider?: 'google' | 'atlasApple';
  accountId?: string | null;
}
