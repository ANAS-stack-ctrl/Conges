import React, { useEffect, useMemo, useState, useCallback } from "react";
import "./DirectorDashboard.css";
import logo from "./assets/logo.png";
import usercircle from "./assets/User.png";
import { getPendingApprovals, actOnApproval, getRoleStats, downloadRequestPdf } from "./admin/api";
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

  const filtered = useMemo(() => {
    if (!filter.trim()) return rows;
    const q = filter.toLowerCase();
    return rows.filter(
      r =>
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
        getPendingApprovals("Director"),
        getRoleStats("Director"),
      ]);
      setRows(data || []);
      setTodayApproved(stats?.approvedToday ?? 0);
      setTodayRejected(stats?.rejectedToday ?? 0);
    } catch (e) {
      console.error(e);
      setErr("Impossible de charger les validations en attente.");
      toast.error("Chargement des demandes en attente impossible.");
    } finally {
      setLoading(false);
    }
  }, [toast]);

  useEffect(() => { load(); }, [load]);

  const openProof = (path) => {
    if (!path) return;
    const url = path.startsWith("http") ? path : `${FILE_BASE}${path.startsWith("/") ? path : `/${path}`}`;
    window.open(url, "_blank", "noopener,noreferrer");
  };

  async function handle(action, id) {
    const ask = await confirm({
      title: action === "Approve" ? "Approuver la demande" : "Rejeter la demande",
      message: action === "Approve"
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
      });
      setRows(prev => prev.filter(r => r.leaveRequestId !== id));
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
          <li onClick={() => navigate("/settings")} style={{ cursor: "pointer" }}>⚙️ Paramètres</li>
          <li onClick={onLogout} style={{ cursor: "pointer" }}>📦 Déconnexion</li>
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
        <p className="subtitle">Demandes en attente de <strong>validation Directeur</strong>.</p>

        <section className="stat-cards">
          <div className="card">🕒 En attente : <strong>{filtered.length}</strong></div>
          <div className="card">✅ Validées aujourd’hui : <strong>{todayApproved}</strong></div>
          <div className="card">❌ Rejetées aujourd’hui : <strong>{todayRejected}</strong></div>
        </section>

        <section className="bloc">
          <div className="bloc-head">
            <h3>À valider</h3>
            <input
              className="search"
              placeholder="Rechercher (nom, type, statut)"
              value={filter}
              onChange={e => setFilter(e.target.value)}
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
                  {filtered.map(r => (
                    <tr key={r.leaveRequestId}>
                      <td>{r.employeeFullName}</td>
                      <td>{r.leaveType?.name}</td>
                      <td>{new Date(r.startDate).toLocaleDateString()}</td>
                      <td>{new Date(r.endDate).toLocaleDateString()}</td>
                      <td>{r.requestedDays}{r.isHalfDay ? " (½)" : ""}</td>
                      <td>{r.leaveType?.approvalFlow}</td>
                      <td>{r.currentStage}</td>
                      <td>
                        {r.proofFilePath ? (
                          <button className="link" onClick={() => openProof(r.proofFilePath)}>Voir</button>
                        ) : "—"}
                      </td>
                      <td>
                        <button className="ghost" title="Télécharger PDF" onClick={() => downloadRequestPdf(r.leaveRequestId)}>📄</button>
                      </td>
                      <td className="actions">
                        {selectedId === r.leaveRequestId ? (
                          <div className="action-area">
                            <textarea
                              placeholder="Commentaire (obligatoire si rejet)"
                              value={comment}
                              onChange={e => setComment(e.target.value)}
                            />
                            <div className="btns">
                              <button
                                disabled={actionBusy}
                                onClick={() => handle("Approve", r.leaveRequestId)}>
                                ✅ Approuver
                              </button>
                              <button
                                disabled={actionBusy || !comment.trim()}
                                onClick={() => handle("Reject", r.leaveRequestId)}>
                                ❌ Rejeter
                              </button>
                              <button className="ghost" onClick={() => { setSelectedId(null); setComment(""); }}>
                                Annuler
                              </button>
                            </div>
                          </div>
                        ) : (
                          <>
                            <button title="Approuver" onClick={() => handle("Approve", r.leaveRequestId)}>✅</button>
                            <button title="Rejeter" onClick={() => setSelectedId(r.leaveRequestId)}>✍️</button>
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
