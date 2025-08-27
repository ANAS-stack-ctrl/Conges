import React, { useEffect, useState } from "react";
import { apiGet, apiPost, apiPut, apiDelete } from "./api";
import { useToast } from "../ui/ToastProvider";
import { useConfirm } from "../ui/ConfirmProvider";

const DEFAULT_DTO = {
  name: "",
  requiresProof: false,
  consecutiveDays: 0,
  approvalFlow: "Serial",
  policyId: 1
};

export default function AdminLeaveTypes() {
  const toast = useToast();
  const confirm = useConfirm().confirm;

  const [items, setItems] = useState([]);
  const [form, setForm] = useState(DEFAULT_DTO);
  const [editingId, setEditingId] = useState(null);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  const load = async () => {
    try {
      setLoading(true);
      setErr("");
      const data = await apiGet("/LeaveType");
      setItems(Array.isArray(data) ? data : []);
    } catch {
      setErr("Impossible de charger les types de congé");
      toast.error("Impossible de charger les types de congé.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []); // mount

  const submit = async (e) => {
    e.preventDefault();
    try {
      if (editingId) {
        await apiPut(`/LeaveType/${editingId}`, form);
        toast.ok("Type de congé mis à jour.");
      } else {
        await apiPost("/LeaveType", form);
        toast.ok("Type de congé créé.");
      }
      setForm(DEFAULT_DTO);
      setEditingId(null);
      await load();
    } catch (e2) {
      toast.error(e2?.message || "Sauvegarde impossible.");
    }
  };

  const edit = (t) => {
    setEditingId(t.leaveTypeId);
    setForm({
      name: t.name,
      requiresProof: t.requiresProof,
      consecutiveDays: t.consecutiveDays ?? 0,
      approvalFlow: t.approvalFlow || "Serial",
      policyId: t.policy?.policyId || 1
    });
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const remove = async (t) => {
    const ok = await confirm({
      title: "Supprimer",
      message: `Supprimer "${t.name}" ?`,
      okText: "Supprimer",
      variant: "danger",
    });
    if (!ok) return;

    try {
      await apiDelete(`/LeaveType/${t.leaveTypeId}`);
      setItems(prev => prev.filter(x => x.leaveTypeId !== t.leaveTypeId));
      toast.ok("Type supprimé.");
    } catch (e2) {
      toast.error(e2?.message || "Suppression impossible.");
    }
  };

  return (
    <div className="page">
      <h2>Types de congé</h2>

      {loading && <p>Chargement…</p>}
      {err && <p className="error">{err}</p>}

      <div className="grid-2">
        <div className="card">
          <h3>{editingId ? "Modifier" : "Ajouter"} un type</h3>
          <form onSubmit={submit} className="form">
            <label>Nom
              <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} required />
            </label>

            <label>Preuve requise ?
              <input type="checkbox"
                     checked={form.requiresProof}
                     onChange={e => setForm({ ...form, requiresProof: e.target.checked })}/>
            </label>

            <label>Jours consécutifs max (0 = illimité)
              <input type="number" min="0"
                     value={form.consecutiveDays}
                     onChange={e => setForm({ ...form, consecutiveDays: parseInt(e.target.value || "0", 10) })}/>
            </label>

            <label>Workflow d’approbation
              <select value={form.approvalFlow}
                      onChange={e => setForm({ ...form, approvalFlow: e.target.value })}>
                <option value="Serial">Serial</option>
                <option value="Parallel">Parallel</option>
              </select>
            </label>

            <label>PolicyId
              <input type="number" min="1"
                     value={form.policyId}
                     onChange={e => setForm({ ...form, policyId: parseInt(e.target.value || "1", 10) })}/>
            </label>

            <div className="row">
              <button type="submit">{editingId ? "Enregistrer" : "Ajouter"}</button>
              {editingId && (
                <button type="button" className="secondary" onClick={() => { setEditingId(null); setForm(DEFAULT_DTO); }}>
                  Annuler
                </button>
              )}
            </div>
          </form>
        </div>

        <div className="card">
          <h3>Liste</h3>
          <table className="table">
            <thead>
              <tr>
                <th>Nom</th><th>Preuve</th><th>Consecutifs</th><th>Flow</th><th></th>
              </tr>
            </thead>
            <tbody>
              {items.map(t => (
                <tr key={t.leaveTypeId}>
                  <td>{t.name}</td>
                  <td>{t.requiresProof ? "Oui" : "Non"}</td>
                  <td>{t.consecutiveDays ?? 0}</td>
                  <td>{t.approvalFlow}</td>
                  <td style={{whiteSpace:"nowrap"}}>
                    <button className="secondary" onClick={() => edit(t)}>Modifier</button>{" "}
                    <button className="danger" onClick={() => remove(t)}>Supprimer</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
