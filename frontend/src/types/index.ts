export interface User {
  id: string;
  email: string;
  displayName: string;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  expiresAtUtc: string;
  user: User;
}

export interface RegisterPayload {
  email: string;
  displayName: string;
  password: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

export interface Note {
  id: string;
  title: string;
  content: string;
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateNotePayload {
  title: string;
  content: string;
  isPinned: boolean;
}

export interface UpdateNotePayload {
  title: string;
  content: string;
  isPinned: boolean;
}

export type SortOption = "newest" | "oldest" | "alphabetical";

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}
