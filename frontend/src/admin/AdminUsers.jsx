import React, { useEffect, useState } from "react";
import { apiGet, apiPut, apiDelete } from "./api";
import { useToast } from "../ui/ToastProvider";
import { useConfirm } from "../ui/ConfirmProvider";

const ROLES = ["Admin", "Employee", "RH", "Manager", "Director"];

export default function AdminUsers() {
  const toast = useToast();
  const confirm = useConfirm().confirm;

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  const load = async () => {
    try {
      setLoading(true);
      setErr("");
      const data = await apiGet("/User");
      setItems(Array.isArray(data) ? data : []);
    } catch (e) {
      setErr("Impossible de charger les utilisateurs");
      toast.error("Impossible de charger les utilisateurs.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const onRoleChange = async (u, role) => {
    try {
      await apiPut(`/User/${u.userId}`, { ...u, role });
      setItems(prev => prev.map(x => x.userId === u.userId ? { ...x, role } : x));
      toast.ok("Rôle mis à jour.");
    } catch {
      toast.error("Échec de mise à jour du rôle.");
    }
  };

  const onDelete = async (u) => {
    const ok = await confirm({
      title: "Supprimer",
      message: `Supprimer ${u.firstName} ${u.lastName} ?`,
      okText: "Supprimer",
      variant: "danger",
    });
    if (!ok) return;

    try {
      await apiDelete(`/User/${u.userId}`);
      setItems(prev => prev.filter(x => x.userId !== u.userId));
      toast.ok("Utilisateur supprimé.");
    } catch {
      toast.error("Suppression impossible (dépendances possibles).");
    }
  };

  return (
    <div className="page">
      <h2>Utilisateurs</h2>
      {loading && <p>Chargement…</p>}
      {err && <p className="error">{err}</p>}

      {!loading && items.length === 0 && <p>Aucun utilisateur.</p>}

      {items.length > 0 && (
        <div className="card">
          <table className="table">
            <thead>
              <tr>
                <th>Nom</th><th>Email</th><th>Rôle</th><th style={{width:130}}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.map(u => (
                <tr key={u.userId}>
                  <td>{u.firstName} {u.lastName}</td>
                  <td>{u.email}</td>
                  <td>
                    <select
                      value={u.role || "Employee"}
                      onChange={e => onRoleChange(u, e.target.value)}
                    >
                      {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
                    </select>
                  </td>
                  <td>
                    <button className="danger" onClick={() => onDelete(u)}>Supprimer</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
