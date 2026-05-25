import React, { createContext, useCallback, useEffect, useMemo, useRef, useState } from "react";

export type DialogVariant = "info" | "success" | "warning" | "error";

type BaseOptions = {
  title?: string;
  message: string;
  variant?: DialogVariant;
  okText?: string;
};

type AlertOptions = BaseOptions;

type ConfirmOptions = BaseOptions & {
  cancelText?: string;
};

type DialogMode = "alert" | "confirm";

type DialogState = {
  mode: DialogMode;
  title: string;
  message: string;
  variant: DialogVariant;
  okText: string;
  cancelText?: string;
  resolve: (value: boolean) => void;
};

export type DialogApi = {
  alert: (options: AlertOptions) => Promise<void>;
  confirm: (options: ConfirmOptions) => Promise<boolean>;
};

export const DialogContext = createContext<DialogApi | null>(null);

const DEFAULT_TITLES: Record<DialogVariant, string> = {
  info: "Thông báo",
  success: "Thành công",
  warning: "Cảnh báo",
  error: "Có lỗi xảy ra"
};

function variantIcon(variant: DialogVariant) {
  switch (variant) {
    case "success":
      return "✓";
    case "warning":
      return "!";
    case "error":
      return "×";
    default:
      return "i";
  }
}

export function DialogProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<DialogState | null>(null);
  const okBtnRef = useRef<HTMLButtonElement | null>(null);

  const close = useCallback(
    (result: boolean) => {
      setState((prev) => {
        if (!prev) return prev;
        prev.resolve(result);
        return null;
      });
    },
    [setState]
  );

  const alert = useCallback((options: AlertOptions) => {
    return new Promise<void>((resolve) => {
      const variant = options.variant ?? "info";
      setState({
        mode: "alert",
        title: options.title ?? DEFAULT_TITLES[variant],
        message: options.message,
        variant,
        okText: options.okText ?? "OK",
        resolve: () => resolve()
      });
    });
  }, []);

  const confirm = useCallback((options: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => {
      const variant = options.variant ?? "info";
      setState({
        mode: "confirm",
        title: options.title ?? DEFAULT_TITLES[variant],
        message: options.message,
        variant,
        okText: options.okText ?? "Đồng ý",
        cancelText: options.cancelText ?? "Hủy",
        resolve
      });
    });
  }, []);

  const api = useMemo<DialogApi>(() => ({ alert, confirm }), [alert, confirm]);

  useEffect(() => {
    if (!state) return;

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        close(false);
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [state, close]);

  useEffect(() => {
    if (!state) return;
    // focus the OK button for accessibility / keyboard flow
    const t = window.setTimeout(() => okBtnRef.current?.focus(), 0);
    return () => window.clearTimeout(t);
  }, [state]);

  return (
    <DialogContext.Provider value={api}>
      {children}
      {state && (
        <div
          className="dialog-overlay"
          role="presentation"
          onMouseDown={(e) => {
            if (e.target === e.currentTarget) close(false);
          }}
        >
          <div className="dialog" role="dialog" aria-modal="true" aria-label={state.title}>
            <div className={`dialog-header ${state.variant}`}>
              <div className={`dialog-icon ${state.variant}`}>{variantIcon(state.variant)}</div>
              <div className="dialog-header-text">
                <div className="dialog-title">{state.title}</div>
              </div>
            </div>

            <div className="dialog-body">{state.message}</div>

            <div className="dialog-actions">
              {state.mode === "confirm" && (
                <button className="ghost-button" type="button" onClick={() => close(false)}>
                  {state.cancelText}
                </button>
              )}
              <button
                ref={okBtnRef}
                className="primary-button"
                type="button"
                onClick={() => close(true)}
              >
                {state.okText}
              </button>
            </div>
          </div>
        </div>
      )}
    </DialogContext.Provider>
  );
}
