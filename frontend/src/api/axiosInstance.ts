import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import type { ProblemDetails } from "../types";

const AUTH_TOKEN_STORAGE_KEY = "notesapp_token";

export const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000/api",
  timeout: 15000,
});

// Attach the JWT bearer token to every outgoing request, when present.
axiosInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Normalize ProblemDetails errors and handle expired/invalid sessions globally.
axiosInstance.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ProblemDetails>) => {
    if (error.response?.status === 401) {
      localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
      window.dispatchEvent(new CustomEvent("notesapp:session-expired"));
    }

    const message =
      error.response?.data?.detail ??
      error.response?.data?.title ??
      error.message ??
      "An unexpected error occurred.";

    return Promise.reject(new Error(message));
  }
);

export function setStoredToken(token: string): void {
  localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token);
}

export function getStoredToken(): string | null {
  return localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
}

export function clearStoredToken(): void {
  localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
}
