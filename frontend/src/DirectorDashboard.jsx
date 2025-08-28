import React, { useEffect, useMemo, useState, useCallback } from "react";
import "./DirectorDashboard.css";
import logo from "./assets/logo.png";
import usercircle from "./assets/User.png";
import {
  getPendingApprovals,
  actOnApproval,
  getRoleStats,
  downloadRequestPdf,
  API_BASE,
  // ↓↓↓ fonctions d’API pour la délégation (ajoute-les dans admin/api)
  dirListMembers,
  dirListDelegations,
  dirCreateDelegation,
  dirEndDelegation,
} from "./admin/api";
import { useNavigate } from "react-router-dom";
import { useConfirm } from "./ui/ConfirmProvider";
import { useToast } from "./ui/ToastProvider";

const FILE_BASE = "https://localhost:7233";

export default function DirectorDashboard({ user, onLogout }) {
  const navigate = useNavigate();
  const confirm = useConfirm().confirm;
  const toast = useToast();

  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");
  const [rows, setRows] = useState([]);
  const [actionBusy, setActionBusy] = useState(false);
  const [comment, setComment] = useState("");
  const [selectedId, setSelectedId] = useState(null);
  const [filter, setFilter] = useState("");

  const [todayApproved, setTodayApproved] = useState(0);
  const [todayRejected, setTodayRejected] = useState(0);

  // ─────────────────────────────────────────────────────────────
  // Bloc "Affectations manager"
  // ─────────────────────────────────────────────────────────────
  const [membersLoading, setMembersLoading] = useState(false);
  const [employees, setEmployees] = useState([]);
  const [managers, setManagers] = useState([]);
  const [assignments, setAssignments] = useState([]);
  const [selectedManager, setSelectedManager] = useState("");
  const [selectedEmployees, setSelectedEmployees] = useState([]);
  const [assignBusy, setAssignBusy] = useState(false);

  // Hiérarchie du directeur (résolue même si pas fournie dans props.user)
  const [resolvedHierarchyId, setResolvedHierarchyId] = useState(
    user?.hierarchyId ?? user?.HierarchyId ?? null
  );

  // ─────────────────────────────────────────────────────────────
  // Bloc "Délégations (managers en congé)"
  // ─────────────────────────────────────────────────────────────
  const [dlLoading, setDlLoading] = useState(false);
  const [dlManagers, setDlManagers] = useState([]);      // mêmes managers que ci-dessus mais via endpoint dirListMembers
  const [delegations, setDelegations] = useState([]);    // liste des délégations
  const [mOnLeave, setMOnLeave] = useState("");          // manager en congé
  const [mDelegate, setMDelegate] = useState("");        // manager délégué
  const [dStart, setDStart] = useState("");
  const [dEnd, setDEnd] = useState("");
  const [creating, setCreating] = useState(false);

  const filtered = useMemo(() => {
    if (!filter.trim()) return rows;
    const q = filter.toLowerCase();
    return rows.filter(
      (r) =>
        (r.employeeFullName || "").toLowerCase().includes(q) ||
        (r.leaveType?.name || "").toLowerCase().includes(q) ||
        (r.status || "").toLowerCase().includes(q)
    );
  }, [rows, filter]);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setErr("");
      const [data, stats] = await Promise.all([
        getPendingApprovals({ role: "Director", reviewerUserId: user?.userId }),
        getRoleStats("Director"),
      ]);

      const safe = Array.isArray(data)
        ? data.filter((d) => d.userId !== user?.userId && d.createdBy !== user?.userId)
        : [];

      setRows(safe);
      setTodayApproved(stats?.approvedToday ?? 0);
      setTodayRejected(stats?.rejectedToday ?? 0);
    } catch (e) {
      console.error(e);
      setErr("Impossible de charger les validations en attente.");
      toast.error("Chargement des demandes en attente impossible.");
    } finally {
      setLoading(false);
    }
  }, [toast, user?.userId]);

  useEffect(() => {
    load();
  }, [load]);

  // si la hiérarchie n'est pas connue côté front, on la récupère
  useEffect(() => {
    let alive = true;
    (async () => {
      if (resolvedHierarchyId || !user?.userId) return;
      try {
        const res = await fetch(`${API_BASE}/User/${user.userId}`);
        if (!res.ok) throw new Error(await res.text());
        const u = await res.json();
        const hId = u?.hierarchyId ?? u?.HierarchyId ?? null;
        if (alive) setResolvedHierarchyId(hId);
      } catch (e) {
        console.warn("Impossible de résoudre la hiérarchie du directeur:", e);
      }
    })();
    return () => { alive = false; };
  }, [resolvedHierarchyId, user?.userId]);

  // charge managers/employés + affectations
  async function loadMembersAndAssignments() {
    if (!resolvedHierarchyId) return;
    setMembersLoading(true);
    try {
      const resMembers = await fetch(`${API_BASE}/Hierarchy/${resolvedHierarchyId}/members`);
      if (!resMembers.ok) throw new Error(await resMembers.text());
      const members = await resMembers.json();

      const emps = members
        .filter((m) => (m.role || m.Role) === "Employee")
        .map((m) => ({
          userId: m.userId ?? m.UserId,
          fullName:
            (m.fullName ?? m.FullName) ||
            `${m.firstName ?? m.FirstName ?? ""} ${m.lastName ?? m.LastName ?? ""}`.trim(),
          email: m.email ?? m.Email,
        }));

      const mgrs = members
        .filter((m) => (m.role || m.Role) === "Manager")
        .map((m) => ({
          userId: m.userId ?? m.UserId,
          fullName:
            (m.fullName ?? m.FullName) ||
            `${m.firstName ?? m.FirstName ?? ""} ${m.lastName ?? m.LastName ?? ""}`.trim(),
          email: m.email ?? m.Email,
        }));

      setEmployees(emps);
      setManagers(mgrs);

      const resAssign = await fetch(`${API_BASE}/ManagerAssignment/hierarchy/${resolvedHierarchyId}`);
      if (!resAssign.ok) throw new Error(await resAssign.text());
      const list = await resAssign.json();

      const norm = Array.isArray(list)
        ? list.map((a) => ({
            managerAssignmentId: a.managerAssignmentId ?? a.ManagerAssignmentId ?? a.id ?? a.Id,
            employeeUserId: a.employeeUserId ?? a.EmployeeUserId,
            employeeName:
              a.employeeName ??
              a.EmployeeName ??
              `${a.employeeFirstName ?? ""} ${a.employeeLastName ?? ""}`.trim(),
            managerUserId: a.managerUserId ?? a.ManagerUserId,
            managerName:
              a.managerName ??
              a.ManagerName ??
              `${a.managerFirstName ?? ""} ${a.managerLastName ?? ""}`.trim(),
          }))
        : [];
      setAssignments(norm);
    } catch (e) {
      console.error(e);
      toast.error("Impossible de charger les membres/affectations de la hiérarchie.");
    } finally {
      setMembersLoading(false);
    }
  }

  useEffect(() => {
    loadMembersAndAssignments();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resolvedHierarchyId]);

  // Affecter plusieurs employés → 1 manager
  async function assignEmployeesToManager() {
    if (!resolvedHierarchyId) {
      toast.warn("Hiérarchie inconnue pour ce directeur.");
      return;
    }
    const managerId = Number(selectedManager || 0);
    if (!managerId) {
      toast.warn("Sélectionnez un manager.");
      return;
    }
    const empIds = selectedEmployees.map((id) => Number(id)).filter(Boolean);
    if (empIds.length === 0) {
      toast.warn("Sélectionnez au moins un employé.");
      return;
    }

    const ask = await confirm({
      title: "Affecter à un manager",
      message: "Confirmez-vous l’affectation du/des employé(s) sélectionné(s) à ce manager ?",
      okText: "Affecter",
      variant: "primary",
    });
    if (!ask) return;

    try {
      setAssignBusy(true);
      const res = await fetch(`${API_BASE}/ManagerAssignment/bulk`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          hierarchyId: resolvedHierarchyId,
          managerUserId: managerId,
          employeeUserIds: empIds,
        }),
      });
      if (!res.ok) throw new Error(await res.text());

      toast.ok("Affectation enregistrée ✅");
      setSelectedEmployees([]);
      await loadMembersAndAssignments();
    } catch (e) {
      console.error(e);
      toast.error(typeof e?.message === "string" && e.message.trim() ? e.message : "Échec de l’affectation.");
    } finally {
      setAssignBusy(false);
    }
  }

  async function removeAssignment(assignmentId) {
    const ask = await confirm({
      title: "Supprimer l’affectation",
      message: "Voulez-vous retirer cette affectation ?",
      okText: "Retirer",
      variant: "danger",
    });
    if (!ask) return;

    try {
      const res = await fetch(`${API_BASE}/ManagerAssignment/${assignmentId}`, { method: "DELETE" });
      if (!res.ok) throw new Error(await res.text());
      toast.ok("Affectation supprimée.");
      await loadMembersAndAssignments();
    } catch (e) {
      console.error(e);
      toast.error("Impossible de supprimer cette affectation.");
    }
  }

  // ─────────────────────────────────────────────────────────────
  // DÉLÉGATIONS — chargement (managers + délégations)
  // ─────────────────────────────────────────────────────────────
  useEffect(() => {
    let alive = true;
    (async () => {
      if (!user?.userId) return;
      try {
        setDlLoading(true);
        const [mem, dels] = await Promise.all([
          dirListMembers(user.userId),       // { managers, employees }
          dirListDelegations(user.userId),   // [{ managerDelegationId, managerName, delegateName, startDate, endDate, active }]
        ]);
        if (!alive) return;
        setDlManagers(mem?.managers || []);
        setDelegations(Array.isArray(dels) ? dels : []);
      } catch (e) {
        console.error(e);
        toast.error("Impossible de charger les délégations.");
      } finally {
        setDlLoading(false);
      }
    })();
    return () => { alive = false; };
  }, [user?.userId, toast]);

  async function createDelegation() {
    if (!mOnLeave || !mDelegate) return toast.warn("Choisissez les deux managers.");
    if (mOnLeave === mDelegate) return toast.warn("Le manager délégué doit être différent.");
    if (!dStart || !dEnd) return toast.warn("Renseignez la période de délégation.");
    if (new Date(dEnd) < new Date(dStart)) return toast.warn("La date de fin doit être ≥ à la date de début.");

    const ask = await confirm({
      title: "Créer une délégation",
      message: "Confirmez-vous la délégation sur la période indiquée ?",
      okText: "Créer",
      variant: "primary",
    });
    if (!ask) return;

    try {
      setCreating(true);
      await dirCreateDelegation({
        directorId: user.userId,
        managerUserId: Number(mOnLeave),
        delegateManagerUserId: Number(mDelegate),
        startDate: dStart,
        endDate: dEnd,
      });
      toast.ok("Délégation créée ✅");
      setMOnLeave("");
      setMDelegate("");
      setDStart("");
      setDEnd("");
      const dels = await dirListDelegations(user.userId);
      setDelegations(Array.isArray(dels) ? dels : []);
    } catch (e) {
      toast.error(e?.message || "Création de la délégation impossible.");
    } finally {
      setCreating(false);
    }
  }

  async function endDelegation(id) {
    const ask = await confirm({
      title: "Terminer la délégation",
      message: "Mettre fin à cette délégation maintenant ?",
      okText: "Terminer",
      variant: "danger",
    });
    if (!ask) return;

    try {
      await dirEndDelegation(id);
      toast.ok("Délégation terminée.");
      const dels = await dirListDelegations(user.userId);
      setDelegations(Array.isArray(dels) ? dels : []);
    } catch (e) {
      toast.error(e?.message || "Fin de la délégation impossible.");
    }
  }

  // Validation classique (inchangée)
  const openProof = (path) => {
    if (!path) return;
    const url = path.startsWith("http") ? path : `${FILE_BASE}${path.startsWith("/") ? path : `/${path}`}`;
    window.open(url, "_blank", "noopener,noreferrer");
  };

  async function handle(action, id) {
    const ask = await confirm({
      title: action === "Approve" ? "Approuver la demande" : "Rejeter la demande",
      message:
        action === "Approve"
          ? "Confirmez-vous l’approbation de cette demande ?"
          : "Confirmez-vous le rejet de cette demande ?",
      okText: action === "Approve" ? "Approuver" : "Rejeter",
      variant: action === "Approve" ? "primary" : "danger",
    });
    if (!ask) return;

    try {
      setActionBusy(true);
      await actOnApproval({
        requestId: id,
        action,
        comment: action === "Reject" ? comment : "",
        actorUserId: user?.userId,
      });
      setRows((prev) => prev.filter((r) => r.leaveRequestId !== id));
      setSelectedId(null);
      setComment("");
      toast.ok(action === "Approve" ? "Demande approuvée." : "Demande rejetée.");

      try {
        const stats = await getRoleStats("Director");
        setTodayApproved(stats?.approvedToday ?? 0);
        setTodayRejected(stats?.rejectedToday ?? 0);
      } catch {}
    } catch (e) {
      toast.error(e?.message || "Erreur lors de l'action.");
    } finally {
      setActionBusy(false);
    }
  }

  return (
    <div className="dir-dashboard">
      <aside className="sidebar">
        <img src={logo} alt="logo" className="logo" />
        <ul>
          <li className="active">📊 Tableau de bord</li>
          <li onClick={() => navigate("/settings")} style={{ cursor: "pointer" }}>
            ⚙️ Paramètres
          </li>
          <li onClick={onLogout} style={{ cursor: "pointer" }}>
            📦 Déconnexion
          </li>
        </ul>
        <footer className="footer">© 2025 – LeaveManager</footer>
      </aside>

      <main className="main-content">
        <header className="topbar">
          <div />
          <div className="user-info">
            <img src={usercircle} alt="user" />
            <span>{user?.fullName || "Directeur"}</span>
          </div>
        </header>

        <h2>Bonjour {user?.fullName || "Directeur"} 👋</h2>
        <p className="subtitle">
          Demandes en attente de <strong>validation Directeur</strong>.
        </p>

        <section className="stat-cards">
          <div className="card">🕒 En attente : <strong>{filtered.length}</strong></div>
          <div className="card">✅ Validées aujourd’hui : <strong>{todayApproved}</strong></div>
          <div className="card">❌ Rejetées aujourd’hui : <strong>{todayRejected}</strong></div>
        </section>

        {/* ─────────────────────────────────────────────────────────
            Bloc DÉLÉGATIONS : manager en congé → manager délégué
           ───────────────────────────────────────────────────────── */}
        <section className="panel" style={{ marginBottom: 18 }}>
          <h3>Délégations (managers en congé)</h3>

          <div className="card" style={{ marginBottom: 12 }}>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr 1fr auto", gap: 12 }}>
              <label style={{ display: "flex", flexDirection: "column" }}>
                Manager en congé
                <select value={mOnLeave} onChange={(e) => setMOnLeave(e.target.value)}>
                  <option value="">— Sélectionner —</option>
                  {dlManagers.map((m) => (
                    <option key={m.UserId || m.userId} value={m.UserId || m.userId}>
                      {(m.fullName || m.FullName) ??
                        `${m.FirstName || m.firstName || ""} ${m.LastName || m.lastName || ""}`}
                    </option>
                  ))}
                </select>
              </label>

              <label style={{ display: "flex", flexDirection: "column" }}>
                Manager délégué
                <select value={mDelegate} onChange={(e) => setMDelegate(e.target.value)}>
                  <option value="">— Sélectionner —</option>
                  {dlManagers.map((m) => (
                    <option key={m.UserId || m.userId} value={m.UserId || m.userId}>
                      {(m.fullName || m.FullName) ??
                        `${m.FirstName || m.firstName || ""} ${m.LastName || m.lastName || ""}`}
                    </option>
                  ))}
                </select>
              </label>

              <label style={{ display: "flex", flexDirection: "column" }}>
                Début
                <input type="date" value={dStart} onChange={(e) => setDStart(e.target.value)} />
              </label>

              <label style={{ display: "flex", flexDirection: "column" }}>
                Fin
                <input type="date" value={dEnd} onChange={(e) => setDEnd(e.target.value)} />
              </label>

              <div style={{ display: "flex", alignItems: "end" }}>
                <button onClick={createDelegation} disabled={creating}>
                  {creating ? "Création…" : "Créer"}
                </button>
              </div>
            </div>
            <p style={{ marginTop: 8, color: "#666" }}>
              Pendant la période, le manager délégué remplace le manager en congé pour valider ses
              demandes et celles des employés qui lui sont associés. À la fin, tout redevient automatique.
            </p>
          </div>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Manager en congé</th>
                  <th>Manager délégué</th>
                  <th>Du</th>
                  <th>Au</th>
                  <th>Statut</th>
                  <th style={{ width: 120 }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {dlLoading ? (
                  <tr><td colSpan={6}>Chargement…</td></tr>
                ) : delegations.length === 0 ? (
                  <tr><td colSpan={6} className="empty">Aucune délégation.</td></tr>
                ) : (
                  delegations.map((d) => (
                    <tr key={d.managerDelegationId || d.ManagerDelegationId}>
                      <td>{d.managerName}</td>
                      <td>{d.delegateName}</td>
                      <td>{new Date(d.startDate).toLocaleDateString()}</td>
                      <td>{new Date(d.endDate).toLocaleDateString()}</td>
                      <td>{d.active ? "Active" : "Terminée"}</td>
                      <td>
                        {d.active ? (
                          <button
                            className="ghost"
                            onClick={() => endDelegation(d.managerDelegationId || d.ManagerDelegationId)}
                          >
                            Terminer
                          </button>
                        ) : "—"}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </section>

        {/* ─────────────────────────────────────────────────────────
            Bloc Affectations Manager ⇢ Employés (existant)
           ───────────────────────────────────────────────────────── */}
        <section className="panel" style={{ marginBottom: 18 }}>
          <h3>Affectations manager (ma hiérarchie)</h3>

          {!resolvedHierarchyId && (
            <div className="card" style={{ marginBottom: 12 }}>
              <em>Vous n’êtes rattaché à aucune hiérarchie (ou elle n’a pas été trouvée).</em>
            </div>
          )}

          <div className="card" style={{ marginBottom: 12 }}>
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "1fr 2fr auto",
                gap: 12,
                alignItems: "end",
              }}
            >
              <label style={{ display: "flex", flexDirection: "column" }}>
                Manager
                <select
                  value={selectedManager}
                  onChange={(e) => setSelectedManager(e.target.value)}
                >
                  <option value="">— Choisir un manager —</option>
                  {managers.map((m) => (
                    <option key={m.userId} value={m.userId}>
                      {m.fullName} ({m.email})
                    </option>
                  ))}
                </select>
              </label>

              <label style={{ display: "flex", flexDirection: "column" }}>
                Employés (multi-sélection)
                <select
                  multiple
                  size={Math.min(8, Math.max(4, employees.length))}
                  value={selectedEmployees}
                  onChange={(e) =>
                    setSelectedEmployees(Array.from(e.target.selectedOptions, (o) => o.value))
                  }
                >
                  {employees.map((u) => (
                    <option key={u.userId} value={u.userId}>
                      {u.fullName} ({u.email})
                    </option>
                  ))}
                </select>
              </label>

              <button
                onClick={assignEmployeesToManager}
                disabled={assignBusy || !selectedManager || selectedEmployees.length === 0}
                style={{ height: 40 }}
              >
                {assignBusy ? "Affectation…" : "Affecter"}
              </button>
            </div>

            <p style={{ marginTop: 8, color: "#666" }}>
              Astuce : maintenez <kbd>Ctrl</kbd>/<kbd>Cmd</kbd> pour sélectionner plusieurs employés.
            </p>
          </div>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Employé</th>
                  <th>Manager assigné</th>
                  <th style={{ width: 120 }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {membersLoading ? (
                  <tr><td colSpan={3}>Chargement…</td></tr>
                ) : assignments.length === 0 ? (
                  <tr><td colSpan={3} className="empty">Aucune affectation pour l’instant.</td></tr>
                ) : (
                  assignments.map((a) => (
                    <tr key={a.managerAssignmentId}>
                      <td>{a.employeeName}</td>
                      <td>{a.managerName}</td>
                      <td>
                        <button
                          className="ghost"
                          onClick={() => removeAssignment(a.managerAssignmentId)}
                          title="Retirer l’affectation"
                        >
                          Retirer
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </section>

        {/* ─────────────────────────────────────────────────────────
            Bloc À valider (inchangé)
           ───────────────────────────────────────────────────────── */}
        <section className="bloc">
          <div className="bloc-head">
            <h3>À valider</h3>
            <input
              className="search"
              placeholder="Rechercher (nom, type, statut)"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
            />
          </div>

          {loading ? (
            <div className="empty">Chargement…</div>
          ) : err ? (
            <div className="error">{err}</div>
          ) : filtered.length === 0 ? (
            <div className="empty">Aucune demande en attente 👍</div>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Employé</th>
                    <th>Type</th>
                    <th>Du</th>
                    <th>Au</th>
                    <th>Jours</th>
                    <th>Flow</th>
                    <th>Étape actuelle</th>
                    <th>Justif.</th>
                    <th>PDF</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((r) => (
                    <tr key={r.leaveRequestId}>
                      <td>{r.employeeFullName}</td>
                      <td>{r.leaveType?.name}</td>
                      <td>{new Date(r.startDate).toLocaleDateString()}</td>
                      <td>{new Date(r.endDate).toLocaleDateString()}</td>
                      <td>
                        {r.requestedDays}
                        {r.isHalfDay ? " (½)" : ""}
                      </td>
                      <td>{r.leaveType?.approvalFlow}</td>
                      <td>{r.currentStage}</td>
                      <td>
                        {r.proofFilePath ? (
                          <button className="link" onClick={() => openProof(r.proofFilePath)}>
                            Voir
                          </button>
                        ) : (
                          "—"
                        )}
                      </td>
                      <td>
                        <button
                          className="ghost"
                          title="Télécharger PDF"
                          onClick={() => downloadRequestPdf(r.leaveRequestId)}
                        >
                          📄
                        </button>
                      </td>
                      <td className="actions">
                        {selectedId === r.leaveRequestId ? (
                          <div className="action-area">
                            <textarea
                              placeholder="Commentaire (obligatoire si rejet)"
                              value={comment}
                              onChange={(e) => setComment(e.target.value)}
                            />
                            <div className="btns">
                              <button
                                disabled={actionBusy}
                                onClick={() => handle("Approve", r.leaveRequestId)}
                              >
                                ✅ Approuver
                              </button>
                              <button
                                disabled={actionBusy || !comment.trim()}
                                onClick={() => handle("Reject", r.leaveRequestId)}
                              >
                                ❌ Rejeter
                              </button>
                              <button
                                className="ghost"
                                onClick={() => {
                                  setSelectedId(null);
                                  setComment("");
                                }}
                              >
                                Annuler
                              </button>
                            </div>
                          </div>
                        ) : (
                          <>
                            <button title="Approuver" onClick={() => handle("Approve", r.leaveRequestId)}>
                              ✅
                            </button>
                            <button title="Rejeter" onClick={() => setSelectedId(r.leaveRequestId)}>
                              ✍️
                            </button>
                          </>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </main>
    </div>
  );
}
