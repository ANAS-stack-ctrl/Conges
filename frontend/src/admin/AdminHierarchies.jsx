import React, { useEffect, useMemo, useState } from "react";
import "./AdminHierarchies.css";
import {
  listHierarchies,
  createHierarchy,
  updateHierarchy,
  deleteHierarchy,
  getHierarchyMembers,
  addHierarchyMember,
  removeHierarchyMember,
  listUsers, // réutilise /api/User
  getHierarchyCandidates, // optionnel, sinon on filtre localement
} from "./api";
import { useToast } from "../ui/ToastProvider";
import { useConfirm } from "../ui/ConfirmProvider";
import { useNavigate } from "react-router-dom";

const ROLE_OPTIONS = ["Employee", "Manager", "Director", "RH"];

export default function AdminHierarchies() {
  const toast = useToast();
  const confirm = useConfirm().confirm;
  const navigate = useNavigate();

  // liste hiérarchies
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  // sélection
  const [selectedId, setSelectedId] = useState(null);
  const selected = useMemo(() => items.find(i => i.hierarchyId === selectedId) || null, [items, selectedId]);

  // formulaire create/edit
  const [form, setForm] = useState({ name: "", code: "", description: "" });
  const [editingId, setEditingId] = useState(null);
  const [saving, setSaving] = useState(false);

  // membres
  const [members, setMembers] = useState([]);
  const [mLoading, setMLoading] = useState(false);
  const [mErr, setMErr] = useState("");

  // ajout membre
  const [roleToAdd, setRoleToAdd] = useState("Employee");
  const [candidateUserId, setCandidateUserId] = useState("");
  const [allUsers, setAllUsers] = useState([]);
  const [loadingCandidates, setLoadingCandidates] = useState(false);

  // filtres
  const [memberFilter, setMemberFilter] = useState("");

  async function loadHierarchies() {
    setLoading(true);
    setErr("");
    try {
      const data = await listHierarchies();
      setItems(Array.isArray(data) ? data : data?.items || []);
      // auto select first
      if (!selectedId && (data?.[0]?.hierarchyId || data?.items?.[0]?.hierarchyId)) {
        setSelectedId(data[0]?.hierarchyId ?? data.items[0]?.hierarchyId);
      }
    } catch (e) {
      setErr(e?.message || "Impossible de charger les hiérarchies.");
      toast.error(e?.message || "Chargement hiérarchies impossible.");
    } finally {
      setLoading(false);
    }
  }

  async function loadMembers(hId) {
    if (!hId) { setMembers([]); return; }
    setMLoading(true);
    setMErr("");
    try {
      const data = await getHierarchyMembers(hId);
      setMembers(Array.isArray(data) ? data : data?.items || []);
    } catch (e) {
      setMErr(e?.message || "Impossible de charger les membres.");
    } finally {
      setMLoading(false);
    }
  }

  // charge initial
  useEffect(() => { loadHierarchies(); }, []); // eslint-disable-line
  useEffect(() => { loadMembers(selectedId); }, [selectedId]); // eslint-disable-line

  // pour l’auto-complétion simple: on prend la liste des users (fallback si pas de /candidates)
  useEffect(() => {
    (async () => {
      try {
        const users = await listUsers();
        setAllUsers(Array.isArray(users) ? users : []);
      } catch {}
    })();
  }, []);

  const startCreate = () => {
    setEditingId(null);
    setForm({ name: "", code: "", description: "" });
  };

  const startEdit = (h) => {
    setEditingId(h.hierarchyId);
    setForm({
      name: h.name || "",
      code: h.code || "",
      description: h.description || "",
    });
  };

  const cancelEdit = () => {
    setEditingId(null);
    setForm({ name: "", code: "", description: "" });
  };

  async function save(e) {
    e?.preventDefault?.();
    if (!form.name.trim()) {
      toast.warn("Le nom est requis.");
      return;
    }
    setSaving(true);
    try {
      if (editingId) {
        await updateHierarchy(editingId, form);
        toast.ok("Hiérarchie mise à jour.");
      } else {
        const { hierarchyId } = await createHierarchy(form);
        toast.ok("Hiérarchie créée.");
        // sélectionner la nouvelle si id renvoyé
        if (hierarchyId) setSelectedId(hierarchyId);
      }
      await loadHierarchies();
      cancelEdit();
    } catch (e) {
      toast.error(e?.message || "Échec de l’enregistrement.");
    } finally {
      setSaving(false);
    }
  }

  async function remove(h) {
    const ok = await confirm({
      title: "Supprimer la hiérarchie",
      message: `Supprimer “${h.name}” ? Cette action est irréversible.`,
      okText: "Supprimer",
      variant: "danger",
    });
    if (!ok) return;
    try {
      await deleteHierarchy(h.hierarchyId);
      toast.ok("Hiérarchie supprimée.");
      // si on supprimait la sélection, on la reset
      if (selectedId === h.hierarchyId) setSelectedId(null);
      await loadHierarchies();
    } catch (e) {
      toast.error(e?.message || "Suppression impossible.");
    }
  }

  // membres: calculs
  const grouped = useMemo(() => {
    const g = { Employee: [], Manager: [], Director: [], RH: [] };
    for (const m of members) {
      const r = m.role || "Employee";
      if (g[r]) g[r].push(m);
      else g.Employee.push(m);
    }
    return g;
  }, [members]);

  const filteredMembers = useMemo(() => {
    const q = memberFilter.trim().toLowerCase();
    if (!q) return members;
    return members.filter(m =>
      (m.firstName + " " + m.lastName).toLowerCase().includes(q) ||
      (m.email || "").toLowerCase().includes(q) ||
      (m.role || "").toLowerCase().includes(q)
    );
  }, [members, memberFilter]);

  // candidats (par API si dispo, sinon on filtre allUsers)
  const [candidateList, setCandidateList] = useState([]);
  useEffect(() => {
    (async () => {
      if (!selectedId) return;
      setLoadingCandidates(true);
      try {
        const role = roleToAdd;
        let data = [];
        try {
          // si l'endpoint existe
          data = await getHierarchyCandidates(selectedId, role);
        } catch {
          // fallback: on enlève ceux déjà membres
          const currentIds = new Set(members.map(m => m.userId));
          data = allUsers.filter(u => !currentIds.has(u.userId));
        }
        setCandidateList(data);
      } catch {
        setCandidateList([]);
      } finally {
        setLoadingCandidates(false);
      }
    })();
  }, [selectedId, roleToAdd, members, allUsers]);

  async function addMember(e) {
    e.preventDefault();
    if (!selectedId) return;
    const uid = Number(candidateUserId);
    if (!uid) return toast.warn("Choisissez un utilisateur.");
    try {
      await addHierarchyMember(selectedId, { userId: uid, role: roleToAdd });
      toast.ok("Membre ajouté.");
      setCandidateUserId("");
      await loadMembers(selectedId);
    } catch (e2) {
      toast.error(e2?.message || "Ajout impossible.");
    }
  }

  async function removeMember(u) {
    if (!selectedId) return;
    const ok = await confirm({
      title: "Retirer le membre",
      message: `Retirer ${u.firstName} ${u.lastName} de cette hiérarchie ?`,
      okText: "Retirer",
      variant: "danger",
    });
    if (!ok) return;
    try {
      await removeHierarchyMember(selectedId, u.userId);
      toast.ok("Membre retiré.");
      await loadMembers(selectedId);
    } catch (e) {
      toast.error(e?.message || "Suppression impossible.");
    }
  }

  return (
    <div className="page hier-page">
      <div className="hier-head">
        <h2>Hiérarchies</h2>
        <div className="actions">
          <button className="ghost" onClick={() => navigate(-1)}>⬅️ Retour</button>
          <button className="btn-purple" onClick={startCreate}>➕ Nouvelle hiérarchie</button>
        </div>
      </div>

      <div className="hier-grid">
        {/* Colonne gauche : liste des hiérarchies */}
        <aside className="hier-list card">
          {loading ? (
            <div>Chargement…</div>
          ) : err ? (
            <div className="error">{err}</div>
          ) : items.length === 0 ? (
            <div>Aucune hiérarchie. Créez-en une.</div>
          ) : (
            <ul>
              {items.map(h => (
                <li
                  key={h.hierarchyId}
                  className={h.hierarchyId === selectedId ? "active" : ""}
                  onClick={() => setSelectedId(h.hierarchyId)}
                >
                  <div className="name">{h.name}</div>
                  <div className="meta">
                    {h.code ? <span className="code">{h.code}</span> : null}
                    {typeof h.membersCount === "number" ? (
                      <span className="pill">{h.membersCount} membres</span>
                    ) : null}
                  </div>
                  <div className="row-actions">
                    <button className="ghost" onClick={(e) => { e.stopPropagation(); startEdit(h); }}>✏️</button>
                    <button className="danger ghost" onClick={(e) => { e.stopPropagation(); remove(h); }}>🗑️</button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </aside>

        {/* Colonne droite : détails / membres */}
        <main className="hier-details">
          {/* Formulaire create/edit */}
          <div className="card">
            <h3>{editingId ? "Modifier la hiérarchie" : "Créer une hiérarchie"}</h3>
            <form className="form" onSubmit={save}>
              <label>Nom
                <input
                  value={form.name}
                  onChange={e => setForm({ ...form, name: e.target.value })}
                  required
                />
              </label>
              <label>Code (facultatif)
                <input
                  value={form.code}
                  onChange={e => setForm({ ...form, code: e.target.value })}
                />
              </label>
              <label>Description
                <textarea
                  rows={2}
                  value={form.description}
                  onChange={e => setForm({ ...form, description: e.target.value })}
                />
              </label>
              <div className="row">
                <button disabled={saving} type="submit">
                  {editingId ? "Enregistrer" : "Créer"}
                </button>
                {editingId && (
                  <button type="button" className="ghost" onClick={cancelEdit}>Annuler</button>
                )}
              </div>
            </form>
          </div>

          {/* Membres */}
          <div className="card">
            <div className="members-head">
              <h3>Membres de la hiérarchie</h3>
              <input
                className="search"
                placeholder="Filtrer (nom, email, rôle)"
                value={memberFilter}
                onChange={e => setMemberFilter(e.target.value)}
              />
            </div>

            {!selectedId ? (
              <div>Sélectionnez une hiérarchie à gauche.</div>
            ) : mLoading ? (
              <div>Chargement des membres…</div>
            ) : mErr ? (
              <div className="error">{mErr}</div>
            ) : (
              <>
                {/* Ajout membre */}
                <form className="inline-form" onSubmit={addMember} style={{ marginBottom: 12 }}>
                  <label>Rôle
                    <select value={roleToAdd} onChange={e => setRoleToAdd(e.target.value)}>
                      {ROLE_OPTIONS.map(r => <option key={r} value={r}>{r}</option>)}
                    </select>
                  </label>
                  <label style={{ flex: 1 }}>Utilisateur
                    <select
                      value={candidateUserId}
                      onChange={e => setCandidateUserId(e.target.value)}
                      disabled={loadingCandidates}
                    >
                      <option value="">— Choisir —</option>
                      {candidateList.map(u => (
                        <option key={u.userId} value={u.userId}>
                          {u.firstName} {u.lastName} ({u.email})
                        </option>
                      ))}
                    </select>
                  </label>
                  <button type="submit" disabled={!candidateUserId}>Ajouter</button>
                </form>

                {/* Tableau des membres (filtré) */}
                <div className="table-wrap">
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Nom</th>
                        <th>Email</th>
                        <th>Rôle</th>
                        <th style={{ width: 120 }}>Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredMembers.length === 0 ? (
                        <tr><td colSpan={4} className="empty">Aucun membre</td></tr>
                      ) : (
                        filteredMembers.map(m => (
                          <tr key={m.userId}>
                            <td>{m.firstName} {m.lastName}</td>
                            <td>{m.email}</td>
                            <td>{m.role}</td>
                            <td className="actions">
                              <button className="danger" onClick={() => removeMember(m)}>Retirer</button>
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>

                {/* Vue groupée par rôle (utile visuellement) */}
                <div className="role-groups">
                  {ROLE_OPTIONS.map(r => (
                    <div key={r} className="role-block">
                      <h4>{r}</h4>
                      <ul>
                        {grouped[r].length === 0 ? (
                          <li className="muted">—</li>
                        ) : grouped[r].map(m => (
                          <li key={m.userId}>{m.firstName} {m.lastName}</li>
                        ))}
                      </ul>
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
        </main>
      </div>
    </div>
  );
}
