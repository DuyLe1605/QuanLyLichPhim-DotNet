import { useQuery } from "@tanstack/react-query";
import { profileApi } from "../api/profile.api";

export const useProfile = () => useQuery({ queryKey: ["profile"], queryFn: profileApi.get });
export const usePoints = () => useQuery({ queryKey: ["points"], queryFn: profileApi.points });
