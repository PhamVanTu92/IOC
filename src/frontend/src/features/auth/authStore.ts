import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthState, AuthUser } from './types';

// ─────────────────────────────────────────────────────────────────────────────
// authStore — persisted Zustand store for authentication state
// Storage key: 'ioc:auth'
// ─────────────────────────────────────────────────────────────────────────────

function isTokenValid(token: string | null, expiresAt: string | null): boolean {
  if (!token || !expiresAt) return false;
  return new Date(expiresAt) > new Date();
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      expiresAt: null,
      isAuthenticated: false,

      login: (token: string, expiresAt: string, user: AuthUser) => {
        set({ token, expiresAt, user, isAuthenticated: true });
      },

      logout: () => {
        set({ token: null, expiresAt: null, user: null, isAuthenticated: false });
      },
    }),
    {
      name: 'ioc:auth',
      // On rehydration: re-evaluate token validity and set isAuthenticated
      onRehydrateStorage: () => (state) => {
        if (state) {
          const valid = isTokenValid(state.token, state.expiresAt);
          state.isAuthenticated = valid;
          if (!valid) {
            state.token = null;
            state.expiresAt = null;
            state.user = null;
          }
        }
      },
    }
  )
);
