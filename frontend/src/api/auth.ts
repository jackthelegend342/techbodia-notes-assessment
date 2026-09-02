import { axiosInstance } from "./axiosInstance";
import type { AuthResponse, LoginPayload, RegisterPayload } from "../types";

export async function registerUser(payload: RegisterPayload): Promise<AuthResponse> {
  const { data } = await axiosInstance.post<AuthResponse>("/auth/register", payload);
  return data;
}

export async function loginUser(payload: LoginPayload): Promise<AuthResponse> {
  const { data } = await axiosInstance.post<AuthResponse>("/auth/login", payload);
  return data;
}
