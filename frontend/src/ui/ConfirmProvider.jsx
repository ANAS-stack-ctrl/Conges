import React, { createContext, useCallback, useContext, useState } from "react";
import "./modal.css";

const ConfirmCtx = createContext(null);

export function useConfirm() {
  const ctx = useContext(ConfirmCtx);
  if (!ctx) throw new Error("useConfirm must be used inside <ConfirmProvider>");
  return ctx;
}

export default function ConfirmProvider({ children }) {
  const [open, setOpen] = useState(false);
  const [opts, setOpts] = useState({ title:"Confirmer", message:"", okText:"Oui", cancelText:"Annuler" });
  const [resolver, setResolver] = useState(null);

  const confirm = useCallback((options) => {
    return new Promise((resolve) => {
      setOpts({
        title: options?.title || "Confirmer l'action",
        message: options?.message || "Voulez-vous continuer ?",
        okText: options?.okText || "Oui",
        cancelText: options?.cancelText || "Annuler",
        variant: options?.variant || "primary", // "danger" pour rouge
      });
      setResolver(() => resolve);
      setOpen(true);
    });
  }, []);

  const onCancel = () => { setOpen(false); resolver && resolver(false); };
  const onOk     = () => { setOpen(false); resolver && resolver(true);  };

  return (
    <ConfirmCtx.Provider value={{ confirm }}>
      {children}

      {open && (
        <div className="modal-backdrop">
          <div className="modal-card">
            <div className="modal-head">
              <h4>{opts.title}</h4>
            </div>
            <div className="modal-body">
              <p>{opts.message}</p>
            </div>
            <div className="modal-foot">
              <button className="btn ghost" onClick={onCancel}>{opts.cancelText}</button>
              <button
                className={`btn ${opts.variant === "danger" ? "danger" : "primary"}`}
                onClick={onOk}
              >
                {opts.okText}
              </button>
            </div>
          </div>
        </div>
      )}
    </ConfirmCtx.Provider>
  );
}
