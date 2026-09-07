import { create } from "zustand";
import type { UserRole } from "../api/types";

interface AuthState {
  token: string | null;
  userId: string | null;
  displayName: string | null;
  role: UserRole | null;
  setAuth: (token: string, userId: string, displayName: string, role: UserRole) => void;
  logout: () => void;
}

const STORAGE_KEY = "sicbo.auth";

function loadInitial(): Pick<AuthState, "token" | "userId" | "displayName" | "role"> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return { token: null, userId: null, displayName: null, role: null };
    return JSON.parse(raw);
  } catch {
    return { token: null, userId: null, displayName: null, role: null };
  }
}

export const useAuthStore = create<AuthState>((set) => ({
  ...loadInitial(),
  setAuth: (token, userId, displayName, role) => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ token, userId, displayName, role }));
    set({ token, userId, displayName, role });
  },
  logout: () => {
    localStorage.removeItem(STORAGE_KEY);
    set({ token: null, userId: null, displayName: null, role: null });
  },
}));
