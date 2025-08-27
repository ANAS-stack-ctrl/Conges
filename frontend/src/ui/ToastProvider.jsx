import React, { createContext, useCallback, useContext, useMemo, useState } from "react";
import "./toast.css";

const ToastCtx = createContext(null);

export function useToast() {
  const ctx = useContext(ToastCtx);
  if (!ctx) throw new Error("useToast must be used inside <ToastProvider>");
  return ctx;
}

let idSeq = 1;

export default function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);

  const remove = useCallback((id) => {
    setToasts((arr) => arr.filter((t) => t.id !== id));
  }, []);

  const push = useCallback((payload) => {
    const t = {
      id: idSeq++,
      type: payload.type || "info", // "success" | "error" | "info" | "warning"
      title: payload.title || "",
      message: payload.message || "",
      timeout: payload.timeout ?? 3500,
    };
    setToasts((arr) => [...arr, t]);
    if (t.timeout > 0) setTimeout(() => remove(t.id), t.timeout);
  }, [remove]);

  const value = useMemo(() => ({
    info:  (m, o={}) => push({ type:"info",    message:m, ...o }),
    ok:    (m, o={}) => push({ type:"success", message:m, ...o }),
    warn:  (m, o={}) => push({ type:"warning", message:m, ...o }),
    error: (m, o={}) => push({ type:"error",   message:m, ...o }),
  }), [push]);

  return (
    <ToastCtx.Provider value={value}>
      {children}
      <div className="toast-stack">
        {toasts.map(t => (
          <div key={t.id} className={`toast toast-${t.type}`}>
            <div className="toast-icon">
              {t.type === "success" ? "✅" : t.type === "error" ? "❌" : t.type === "warning" ? "⚠️" : "ℹ️"}
            </div>
            <div className="toast-body">
              {t.title && <div className="toast-title">{t.title}</div>}
              <div>{t.message}</div>
            </div>
            <button className="toast-close" onClick={() => remove(t.id)}>×</button>
          </div>
        ))}
      </div>
    </ToastCtx.Provider>
  );
}
