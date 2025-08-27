import React, { useEffect, useMemo, useState } from "react";
import "../AdminDashboard.css";
import logo from "../assets/logo.png";
import usercircle from "../assets/User.png";
import { useNavigate } from "react-router-dom";
import { useToast } from "../ui/ToastProvider";
import { useConfirm } from "../ui/ConfirmProvider";

const API_BASE =
  process.env.REACT_APP_API_URL?.replace(/\/$/, "") || "https://localhost:7233";

export default function AdminHolidays({ user, onLogout }) {
  const nav = useNavigate();
  const toast = useToast();
  const confirm = useConfirm().confirm;

  const [rows, setRows] = useState([]);
  const [q, setQ] = useState("");
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  // Formulaire
  const [editId, setEditId] = useState(null);
  const [date, setDate] = useState("");
  const [desc, setDesc] = useState("");
  const [isRecurring, setIsRecurring] = useState(false);
  const [days, setDays] = useState(1);

  const filtered = useMemo(() => {
    const s = q.trim().toLowerCase();
    if (!s) return rows;
    return rows.filter(r =>
      (r.description || "").toLowerCase().includes(s)
      || new Date(r.date).toLocaleDateString().includes(s)
      || (r.isRecurring ? "recurrent" : "ponctuel").includes(s)
    );
  }, [rows, q]);

  async function load() {
    try {
      setLoading(true); setErr("");
      const res = await fetch(`${API_BASE}/api/Holiday`);
      if (!res.ok) throw new Error("Chargement impossible");
      setRows(await res.json());
    } catch (e) {
      setErr(e.message || "Erreur de chargement");
      toast.error(e.message || "Erreur de chargement");
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => { load(); }, []); // mount

  function resetForm() {
    setEditId(null);
    setDate("");
    setDesc("");
    setIsRecurring(false);
    setDays(1);
  }

  async function save(e) {
    e?.preventDefault?.();
    if (!date || !desc.trim()) {
      toast.info("Date et description sont obligatoires.");
      return;
    }
    const payload = {
      date: new Date(date).toISOString(),
      description: desc.trim(),
      isRecurring,
      durationDays: Math.max(1, Number(days)||1)
    };

    try {
      const url = editId ? `${API_BASE}/api/Holiday/${editId}` : `${API_BASE}/api/Holiday`;
      const method = editId ? "PUT" : "POST";
      const res = await fetch(url, {
        method, headers:{ "Content-Type":"application/json" },
        body: JSON.stringify(payload)
      });
      if (!res.ok) throw new Error(editId ? "Mise à jour impossible" : "Création impossible");
      toast.ok(editId ? "Jour férié mis à jour." : "Jour férié créé.");
      resetForm();
      await load();
    } catch (e) {
      toast.error(e.message || "Erreur de sauvegarde");
    }
  }

  async function del(id) {
    const ok = await confirm({
      title: "Supprimer",
      message: "Supprimer ce jour férié ?",
      okText: "Supprimer",
      variant: "danger",
    });
    if (!ok) return;

    try {
      const res = await fetch(`${API_BASE}/api/Holiday/${id}`, { method:"DELETE" });
      if (!res.ok) throw new Error("Suppression impossible");
      toast.ok("Supprimé.");
      await load();
    } catch (e) {
      toast.error(e.message || "Erreur de suppression");
    }
  }

  function startEdit(h) {
    setEditId(h.holidayId);
    setDate(new Date(h.date).toISOString().slice(0,10));
    setDesc(h.description || "");
    setIsRecurring(!!h.isRecurring);
    setDays(h.durationDays || 1);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  return (
    <div className="admin-dashboard">
      <aside className="sidebar">
        <img src={logo} alt="logo" className="logo" />
        <ul>
          <li onClick={() => nav("/admin")} style={{cursor:"pointer"}}>📊 Tableau de bord</li>
          <li className="active">📅 Jours fériés</li>
          <li onClick={() => nav("/admin/leave-types")} style={{cursor:"pointer"}}>🏷️ Types de congé</li>
          <li onClick={() => nav("/admin/pdf-template")} style={{cursor:"pointer"}}>📄 Modèle PDF</li>
          <li onClick={() => nav("/settings")} style={{cursor:"pointer"}}>⚙️ Paramètres</li>
          <li onClick={onLogout} style={{cursor:"pointer"}}>📦 Déconnexion</li>
        </ul>
        <footer className="footer">© 2025 – LeaveManager</footer>
      </aside>

      <main className="main-content">
        <header className="dashboard-header">
          <h2>Gestion des jours fériés</h2>
          <div className="user-info">
            <img src={usercircle} alt="user" />
            <span>{user?.fullName || "Admin"}</span>
          </div>
        </header>

        <section className="panel">
          <h3>{editId ? "Modifier" : "Ajouter"} un jour férié</h3>
          <form className="inline-form" onSubmit={save}>
            <label>Date
              <input type="date" value={date} onChange={e=>setDate(e.target.value)} required />
            </label>
            <label>Durée (jours)
              <input type="number" min="1" value={days} onChange={e=>setDays(e.target.value)} style={{width:100}} />
            </label>
            <label>Récurrent
              <input type="checkbox" checked={isRecurring} onChange={e=>setIsRecurring(e.target.checked)} />
            </label>
            <input
              type="text"
              placeholder="Description"
              value={desc}
              onChange={e=>setDesc(e.target.value)}
              style={{minWidth:260, flex:1}}
              required
            />
            <button type="submit">{editId ? "💾 Mettre à jour" : "➕ Ajouter"}</button>
            {editId && <button type="button" className="ghost" onClick={resetForm}>Annuler</button>}
          </form>
        </section>

        <section className="panel" style={{marginTop:12}}>
          <div className="bloc-head">
            <h3>Jours fériés</h3>
            <input className="search" placeholder="Rechercher…" value={q} onChange={e=>setQ(e.target.value)} />
          </div>

          {loading ? (
            <div className="empty">Chargement…</div>
          ) : err ? (
            <div className="error">{err}</div>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Durée</th>
                    <th>Type</th>
                    <th>Description</th>
                    <th style={{width:160}}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(h => (
                    <tr key={h.holidayId}>
                      <td>{new Date(h.date).toLocaleDateString()}</td>
                      <td>{h.durationDays || 1}</td>
                      <td>{h.isRecurring ? "Récurrent" : "Ponctuel"}</td>
                      <td>{h.description}</td>
                      <td className="actions">
                        <button onClick={() => startEdit(h)}>✏️ Éditer</button>
                        <button className="ghost danger" onClick={() => del(h.holidayId)}>🗑️ Supprimer</button>
                      </td>
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr><td colSpan={5} className="empty">Aucun résultat</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </main>
    </div>
  );
}
