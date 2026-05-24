import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../api/client";
import type { Snack } from "../lib/types";

export function useSnacks() {
  return useQuery<Snack[]>({
    queryKey: ["snacks"],
    queryFn: async () => {
      const res = await apiClient.get("/snacks");
      return res.data;
    }
  });
}
