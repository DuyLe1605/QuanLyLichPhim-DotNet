import { apiClient } from "./client";
import type { Customer, PointTransaction } from "../lib/types";

export const profileApi = {
  get: async () => (await apiClient.get<Customer>("/profile")).data,
  update: async (input: Pick<Customer, "fullName" | "email" | "phone">) =>
    (await apiClient.put<Customer>("/profile", input)).data,
  points: async () => (await apiClient.get<PointTransaction[]>("/profile/points")).data
};
