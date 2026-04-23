export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  role: 'admin' | 'editor' | 'viewer';
  tenantId: string;
}

export interface AuthState {
  user: AuthUser | null;
  token: string | null;
  expiresAt: string | null; // ISO string
  isAuthenticated: boolean;
  // actions
  login: (token: string, expiresAt: string, user: AuthUser) => void;
  logout: () => void;
}
