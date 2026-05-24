import { apiClient } from "./client";
import type { AuthResponse, LoginInput, RegisterInput } from "../lib/types";

export const authApi = {
  login: async (input: LoginInput) => (await apiClient.post<AuthResponse>("/auth/login", input)).data,
  register: async (input: RegisterInput) => (await apiClient.post<AuthResponse>("/auth/register", input)).data,
  me: async () => (await apiClient.get<AuthResponse["customer"]>("/auth/me")).data
};
