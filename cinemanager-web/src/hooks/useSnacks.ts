import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../api/client";
import type { Snack } from "../lib/types";

type SnackApiDto = {
    id: number;
    name: string;
    category: string;
    price: number;
    imagePath?: string;
};

export function useSnacks() {
    return useQuery<Snack[]>({
        queryKey: ["snacks"],
        queryFn: async () => {
            const res = await apiClient.get<SnackApiDto[]>("/snacks");
            return res.data.map((s) => ({
                id: s.id,
                name: s.name,
                description: undefined,
                price: s.price,
                imageUrl: s.imagePath ? `Snacks/${s.imagePath.replace(/\\/g, "/")}` : undefined,
                type: s.category,
            }));
        },
    });
}
