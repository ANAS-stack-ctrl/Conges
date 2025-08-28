import React, { useEffect, useState, useMemo } from "react";
import { apiGet, apiDelete, updateUser } from "./api";
import { useToast } from "../ui/ToastProvider";
import { useConfirm } from "../ui/ConfirmProvider";

const ROLES = ["Employee", "Manager", "Director", "RH", "Admin"];

export default function AdminUsers() {
  const toast = useToast();
  const confirm = useConfirm().confirm;

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");
  const [hierarchies, setHierarchies] = useState([]);
  const [q, setQ] = useState("");

  async function load() {
    try {
      setLoading(true);
      setErr("");
      const [users, hiers] = await Promise.all([apiGet("/User"), apiGet("/Hierarchy")]);
      setItems(Array.isArray(users) ? users : []);
      setHierarchies(Array.isArray(hiers) ? hiers : []);
    } catch (e) {
      setErr("Impossible de charger les utilisateurs");
      toast.error("Chargement impossible.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const filtered = useMemo(() => {
    const s = q.trim().toLowerCase();
    if (!s) return items;
    return items.filter(
      (u) =>
        `${u.firstName} ${u.lastName}`.toLowerCase().includes(s) ||
        (u.email || "").toLowerCase().includes(s) ||
        (u.role || "").toLowerCase().includes(s)
    );
  }, [items, q]);

  const onRoleChange = async (u, role) => {
    try {
      await updateUser(u.userId, { role }); // payload minimal
      setItems((prev) => prev.map((x) => (x.userId === u.userId ? { ...x, role } : x)));
      toast.ok("Rôle mis à jour.");
    } catch (e) {
      toast.error(typeof e?.message === "string" ? e.message : "Échec de mise à jour du rôle.");
    }
  };

  const onHierarchyChange = async (u, value) => {
    // value = "" (retirer) ou "2" (setter)
    try {
      const payload = { hierarchyId: value === "" ? "" : String(value) };
      await updateUser(u.userId, payload);

      setItems((prev) =>
        prev.map((x) =>
          x.userId === u.userId
            ? { ...x, hierarchyId: value === "" ? null : Number(value) }
            : x
        )
      );
      toast.ok("Hiérarchie mise à jour.");
    } catch (e) {
      toast.error(typeof e?.message === "string" ? e.message : "Échec de mise à jour de la hiérarchie.");
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
      setItems((prev) => prev.filter((x) => x.userId !== u.userId));
      toast.ok("Utilisateur supprimé.");
    } catch {
      toast.error("Suppression impossible (dépendances possibles).");
    }
  };

  return (
    <div className="page">
      <h2>Utilisateurs</h2>

      <div className="card" style={{ marginBottom: 12 }}>
        <input
          className="search"
          placeholder="Rechercher (nom, email, rôle)"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
      </div>

      {loading && <p>Chargement…</p>}
      {err && <p className="error">{err}</p>}

      {!loading && filtered.length === 0 && <p>Aucun utilisateur.</p>}

      {filtered.length > 0 && (
        <div className="card">
          <table className="table">
            <thead>
              <tr>
                <th>Nom</th>
                <th>Email</th>
                <th>Rôle</th>
                <th>Hiérarchie</th>
                <th style={{ width: 130 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((u) => (
                <tr key={u.userId}>
                  <td>{u.firstName} {u.lastName}</td>
                  <td>{u.email}</td>
                  <td>
                    <select
                      value={u.role || "Employee"}
                      onChange={(e) => onRoleChange(u, e.target.value)}
                    >
                      {ROLES.map((r) => (
                        <option key={r} value={r}>{r}</option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <select
                      value={u.hierarchyId ?? ""} // important: "" quand null
                      onChange={(e) => onHierarchyChange(u, e.target.value)}
                    >
                      <option value="">—</option>
                      {hierarchies.map((h) => (
                        <option key={h.hierarchyId} value={h.hierarchyId}>
                          {h.code || h.name}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <button className="danger" onClick={() => onDelete(u)}>
                      Supprimer
                    </button>
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
