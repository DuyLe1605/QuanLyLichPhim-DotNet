import { useContext } from "react";
import { DialogContext } from "../providers/DialogProvider";

export function useDialog() {
  const value = useContext(DialogContext);
  if (!value) throw new Error("useDialog must be used inside DialogProvider");
  return value;
}
