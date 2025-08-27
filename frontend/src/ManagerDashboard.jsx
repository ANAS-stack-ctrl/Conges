import React, { useCallback, useEffect, useMemo, useState } from "react";
import "./ManagerDashboard.css";
import logo from "./assets/logo.png";
import usercircle from "./assets/User.png";
import { getPendingApprovals, actOnApproval, getRoleStats } from "./admin/api";
import { useNavigate } from "react-router-dom";
import { useConfirm } from "./ui/ConfirmProvider";
import { useToast } from "./ui/ToastProvider";

const API_BASE =
  process.env.REACT_APP_API_URL?.replace(/\/$/, "") || "https://localhost:7233";
const FILE_BASE = API_BASE;

// même logique que l'EmployeeDashboard
const isApproved = (status) => {
  const s = (status || "").toLowerCase();
  return s.includes("approuv") || s.includes("valid");
};

export default function ManagerDashboard({ user, onLogout }) {
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

  // ➕ Infos perso du manager (comme EmployeeDashboard)
  const [myAnnualBalance, setMyAnnualBalance] = useState(null); // nombre ou null
  const [recentRequests, setRecentRequests] = useState([]);     // tableau
  const [auxLoading, setAuxLoading] = useState(true);

  // ---------- Helpers
  const openProof = (path) => {
    if (!path) return;
    const url = path.startsWith("http")
      ? path
      : `${FILE_BASE}${path.startsWith("/") ? path : `/${path}`}`;
    window.open(url, "_blank", "noopener,noreferrer");
  };

  const openPdf = (id) => {
    const url = `${FILE_BASE}/api/export/leave-request/${id}`;
    window.open(url, "_blank", "noopener,noreferrer");
  };

  // ---------- Charge la file d’attente + stats jour
  const loadQueue = useCallback(async () => {
    setLoading(true);
    setErr("");
    try {
      const [data, stats] = await Promise.all([
        getPendingApprovals({
          userId: user?.userId,
          role: user?.role || "Manager",
        }),
        getRoleStats("Manager"),
      ]);
      setRows(data || []);
      setTodayApproved(stats?.approvedToday ?? 0);
      setTodayRejected(stats?.rejectedToday ?? 0);
    } catch (e) {
      console.error(e);
      setErr("Impossible de charger les validations en attente.");
    } finally {
      setLoading(false);
    }
  }, [user?.userId, user?.role]);

  // ---------- Charge solde (même logique qu'EmployeeDashboard) + dernières demandes
  const loadPersonalBlocks = useCallback(async () => {
    if (!user?.userId) return;
    setAuxLoading(true);

    // 1) Solde de congé annuel, même stratégie que l’EmployeeDashboard
    async function fetchAnnualBalance() {
      try {
        const res = await fetch(`${API_BASE}/api/LeaveBalance/user/${user.userId}`);
        if (!res.ok) throw new Error("Récupération du solde impossible");
        const raw = await res.json();

        const items = Array.isArray(raw)
          ? raw.map(x => ({
              leaveTypeId: x.leaveTypeId ?? x.LeaveTypeId,
              leaveType:   x.leaveType   ?? x.LeaveType,
              balance:     x.balance     ?? x.Balance,
            }))
          : [];

        // on cherche le "Congé annuel" par nom (insensible à la casse) ou par id (si tu as fixé l'id=2)
        const annual = items.find(i =>
          (typeof i.leaveType === "string" && i.leaveType.toLowerCase() === "congé annuel") ||
          i.leaveTypeId === 2
        );

        setMyAnnualBalance(annual ? Number(annual.balance) : 0);
      } catch (e) {
        // fallback éventuel : endpoint total (au cas où tu veux sommer)
        try {
          const r2 = await fetch(`${API_BASE}/api/LeaveBalanceAdjustment/user/${user.userId}/current-balance`);
          if (r2.ok) {
            const j = await r2.json();
            const total = Number(j?.balance ?? 0);
            // si tu préfères afficher *uniquement* l’annuel, commente la ligne suivante
            setMyAnnualBalance(total);
            return;
          }
        } catch {}
        setMyAnnualBalance(0);
      }
    }

    // 2) Dernières demandes (comme EmployeeDashboard -> /api/LeaveRequest/user/{id})
    async function fetchRecent() {
      try {
        const res = await fetch(`${API_BASE}/api/LeaveRequest/user/${user.userId}`);
        if (!res.ok) throw new Error();
        const json = await res.json();
        setRecentRequests(Array.isArray(json) ? json.slice(0, 10) : []);
      } catch {
        setRecentRequests([]);
      }
    }

    await Promise.allSettled([fetchAnnualBalance(), fetchRecent()]);
    setAuxLoading(false);
  }, [user?.userId]);

  useEffect(() => { loadQueue(); }, [loadQueue]);
  useEffect(() => { loadPersonalBlocks(); }, [loadPersonalBlocks]);

  // ---------- Filtre tableau à valider
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

  // ---------- Actions
  async function handle(action, id) {
    const ask = await confirm({
      title: action === "Approve" ? "Approuver la demande" : "Rejeter la demande",
      message:
        action === "Approve"
          ? "Êtes-vous sûr de vouloir approuver cette demande ?"
          : "Êtes-vous sûr de vouloir rejeter cette demande ?",
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
        const stats = await getRoleStats("Manager");
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
    <div className="mgr-dashboard">
      <aside className="sidebar">
        <img src={logo} alt="logo" className="logo" />
        <ul>
          <li className="active">📊 Tableau de bord</li>
          <li onClick={() => navigate("/manager/new-request")} style={{ cursor: "pointer" }}>
            ➕ Nouvelle demande
          </li>
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
          <div className="header-actions" style={{ display: "flex", gap: 8 }}>

          </div>

          <div className="user-info">
            <img src={usercircle} alt="user" />
            <span>{user?.fullName || "Manager"}</span>
          </div>
        </header>

        <h2>Bonjour {user?.fullName || "Manager"} 👋</h2>
        <p className="subtitle">
          Demandes en attente de <strong>validation Manager</strong>.
        </p>

        {/* ---- Bloc infos perso (même rendu que l’EmployeeDashboard) */}
        <section className="panel" style={{ marginBottom: 16 }}>
          <h3>Votre activité</h3>
          {auxLoading ? (
            <div className="card">Chargement…</div>
          ) : (
            <>
              <div className="card" style={{ marginBottom: 12 }}>
                <strong>Solde Congé restant</strong>
                <div style={{ marginTop: 6 }}>
                  → Solde actuel :{" "}
                  <strong>
                    {myAnnualBalance !== null
                      ? `${myAnnualBalance} jour${myAnnualBalance > 1 ? "s" : ""}`
                      : "—"}
                  </strong>
                </div>
              </div>

              <div className="panel">
                <h4>Vos demandes récentes :</h4>
                {recentRequests.length === 0 ? (
                  <div className="empty">Aucune demande</div>
                ) : (
                  <div className="table-wrap">
                    <table className="table">
                      <thead>
                        <tr>
                          <th>Type de congé</th>
                          <th>Du</th>
                          <th>Au</th>
                          <th>Jours</th>
                          <th>Statut</th>
                          <th>PDF</th>
                        </tr>
                      </thead>
                      <tbody>
                        {recentRequests.map((r) => (
                          <tr key={r.leaveRequestId}>
                            <td>{r.leaveType?.name ?? "—"}</td>
                            <td>{new Date(r.startDate).toLocaleDateString()}</td>
                            <td>{new Date(r.endDate).toLocaleDateString()}</td>
                            <td>{r.isHalfDay ? 0.5 : r.requestedDays}</td>
                            <td>
                              {r.status === "Approuvée"
                                ? "✅ Approuvée"
                                : r.status === "Refusée"
                                ? "❌ Refusée"
                                : r.status || "—"}
                            </td>
                            <td style={{ textAlign: "center" }}>
                              {isApproved(r.status) ? (
                                <button
                                  className="icon-btn"
                                  title="Télécharger le PDF"
                                  onClick={() => openPdf(r.leaveRequestId)}
                                >
                                  🧾
                                </button>
                              ) : (
                                "—"
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </>
          )}
        </section>

        {/* ---- Stats de validation du jour */}
        <section className="stat-cards">
          <div className="card">
            🕒 En attente : <strong>{filtered.length}</strong>
          </div>
          <div className="card">
            ✅ Validées aujourd’hui : <strong>{todayApproved}</strong>
          </div>
          <div className="card">
            ❌ Rejetées aujourd’hui : <strong>{todayRejected}</strong>
          </div>
        </section>

        {/* ---- Tableau 'À valider' */}
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
                            <button
                              title="Approuver"
                              onClick={() => handle("Approve", r.leaveRequestId)}
                            >
                              ✅
                            </button>
                            <button
                              title="Rejeter (ajouter un commentaire)"
                              onClick={() => setSelectedId(r.leaveRequestId)}
                            >
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
