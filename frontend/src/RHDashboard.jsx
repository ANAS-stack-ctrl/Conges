import React, { useEffect, useMemo, useState } from "react";
import "./RHDashboard.css";
import logo from "./assets/logo.png";
import userIcon from "./assets/User.png";
import { useNavigate } from "react-router-dom";
import {
  rhListRequests,
  rhStats,
  getApprovalHistory,
  actOnApproval,
  downloadRequestPdf,
} from "./admin/api";
import { useConfirm } from "./ui/ConfirmProvider";
import { useToast } from "./ui/ToastProvider";

const FILE_BASE = "https://localhost:7233";

const RHDashboard = ({ user, onLogout }) => {
  const navigate = useNavigate();
  const confirm = useConfirm().confirm;
  const toast = useToast();

  const [list, setList] = useState([]);
  const [stats, setStats] = useState({ total: 0, valides: 0, refusees: 0, attente: 0 });
  const [q, setQ] = useState("");

  const [detail, setDetail] = useState(null);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [errorDetail, setErrorDetail] = useState("");

  const [actionBusy, setActionBusy] = useState(false);
  const [comment, setComment] = useState("");

  const [previewUrl, setPreviewUrl] = useState("");
  const [showPreview, setShowPreview] = useState(false);

  // ————— Utilitaires d’accès champ (compat ancienne API)
  const getName = (row) => row.name ?? row.employee ?? "";
  const getRole = (row) => row.role ?? row.userRole ?? ""; // fallback si plus tard
  const getHierarchy = (row) => row.hierarchy ?? row.hierarchyName ?? "";

  const filtered = useMemo(() => {
    const s = q.trim().toLowerCase();
    if (!s) return list;
    return list.filter((r) =>
      (getName(r) || "").toLowerCase().includes(s) ||
      (getRole(r) || "").toLowerCase().includes(s) ||
      (getHierarchy(r) || "").toLowerCase().includes(s) ||
      (r.type || "").toLowerCase().includes(s) ||
      (r.status || "").toLowerCase().includes(s)
    );
  }, [list, q]);

  async function loadEverything() {
    const [l, st] = await Promise.all([rhListRequests(), rhStats()]);
    setList(Array.isArray(l) ? l : []);
    setStats(st || { total: 0, valides: 0, refusees: 0, attente: 0 });
  }
  useEffect(() => { loadEverything().catch(console.error); }, []);

  async function openDetail(leaveRequestId) {
    try {
      setLoadingDetail(true);
      setErrorDetail("");
      const data = await getApprovalHistory(leaveRequestId);
      setDetail(data || null);
    } catch (e) {
      setErrorDetail(e?.message || "Impossible de charger le détail.");
    } finally {
      setLoadingDetail(false);
    }
  }

  function openProof(rawPath) {
    if (!rawPath) { toast.info("Aucun justificatif."); return; }
    const url = rawPath.startsWith("http") ? rawPath : `${FILE_BASE}${rawPath}`;
    setPreviewUrl(url); setShowPreview(true);
  }
  function closePreview() { setShowPreview(false); setPreviewUrl(""); }
  function renderPreview() {
    if (!previewUrl) return null;
    const isPdf = previewUrl.toLowerCase().endsWith(".pdf");
    return isPdf
      ? <iframe title="Justificatif" src={previewUrl} style={{ width:"90vw", height:"80vh", border:"none" }} />
      : <img src={previewUrl} alt="Justificatif" style={{ maxWidth:"90vw", maxHeight:"80vh", display:"block" }} />;
  }

  async function handleAction(action) {
    if (!detail?.request?.leaveRequestId) return;

    const ask = await confirm({
      title: action === "Approve" ? "Validation RH" : "Rejet RH",
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
        requestId: detail.request.leaveRequestId,
        action,
        comment: action === "Reject" ? comment : "",
        role: "RH",
        actorUserId: user?.userId,
      });
      setDetail(null); setComment("");
      await loadEverything();
      toast.ok(action === "Approve" ? "Demande approuvée par RH." : "Demande rejetée par RH.");
    } catch (e) {
      toast.error(e?.message || "Erreur lors de l'action RH.");
    } finally {
      setActionBusy(false);
    }
  }

  const pct = stats.total ? ((stats.valides / stats.total) * 100).toFixed(2) : "0.00";
  const emoji = (s) => s === "Approuvée" ? "✅" : s === "En attente" ? "⏳" : s === "Refusée" ? "❌" : "";

  return (
    <div className="rh-dashboard">
      <aside className="sidebar">
        <img src={logo} alt="logo" className="logo" />
        <ul>
          <li>📄 Toutes les demandes</li>
          <li>📊 Statistiques</li>
          {/* ➕ Bouton déconnexion */}
    <li onClick={onLogout} style={{ cursor: "pointer" }}>
      📦 Déconnexion
    </li>
        </ul>
        <footer className="footer">© 2025 – LeaveManager</footer>
      </aside>

      <main className="main-content">
        <header className="dashboard-header">
          <div />
          <div className="user-info">
            <span>{user?.fullName || "RH"}</span>
            <img src={userIcon} alt="user" />
          </div>
        </header>

        <h2>Nombre total de demandes: {stats.total}</h2>
        <p>✅ Validées : {stats.valides} — ❌ Refusées : {stats.refusees} — ⏳ En attente : {stats.attente}</p>
        <p>📊 Taux de validation : {pct}%</p>

        <div className="toolbar">
          <input placeholder="Rechercher (nom, rôle, hiérarchie, type…)" value={q} onChange={(e)=>setQ(e.target.value)} />
          <button className="exporter-btn">📤 Exporter</button>
        </div>

        <h3>Tableau de demandes :</h3>
        <table>
          <thead>
            <tr>
              <th>Nom</th>
              <th>Rôle</th>
              <th>Hiérarchie</th>
              <th>Type</th>
              <th>Du</th>
              <th>Au</th>
              <th>Statut</th>
              <th>Justif.</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((d) => (
              <tr key={d.leaveRequestId}>
                <td>{getName(d)}</td>
                <td>{getRole(d) || "—"}</td>
                <td>{getHierarchy(d) || "—"}</td>
                <td>{d.type}</td>
                <td>{new Date(d.startDate).toLocaleDateString()}</td>
                <td>{new Date(d.endDate).toLocaleDateString()}</td>
                <td>{emoji(d.status)} {d.status}</td>
                <td>
                  {d.proofFilePath ? (
                    <button type="button" onClick={() => openProof(d.proofFilePath)} className="linklike" title="Voir justificatif">
                      👁️ Voir
                    </button>
                  ) : "—"}
                </td>
                <td><button onClick={() => openDetail(d.leaveRequestId)}>Détails</button></td>
              </tr>
            ))}
          </tbody>
        </table>

        {detail && (
          <div className="popup">
            <h4>Détails de la demande</h4>
            {loadingDetail ? (
              <p>Chargement des détails…</p>
            ) : errorDetail ? (
              <p className="error">{errorDetail}</p>
            ) : (
              <>
                <div className="req">
                  <p><strong>Employé :</strong> {detail.request.employee}</p>
                  <p><strong>Type :</strong> {detail.request.type}</p>
                  <p><strong>Du :</strong> {new Date(detail.request.startDate).toLocaleDateString()}</p>
                  <p><strong>Au :</strong> {new Date(detail.request.endDate).toLocaleDateString()}</p>
                  <p><strong>Jours :</strong> {detail.request.requestedDays}</p>
                  <p><strong>Statut :</strong> {detail.request.status}</p>
                  <p><strong>Justificatif :</strong>{" "}
                    {detail.request.proofFilePath ? (
                      <button type="button" className="linklike" onClick={() => openProof(detail.request.proofFilePath)}>👁️ Voir</button>
                    ) : "—"}
                  </p>
                </div>

                <h5>Historique des validations</h5>
                <div className="table-wrap">
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Niveau</th>
                        <th>Statut</th>
                        <th>Par</th>
                        <th>Date</th>
                        <th>Commentaires</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detail.approvals?.map((a) => (
                        <tr key={a.approvalId}>
                          <td>{a.level}</td>
                          <td>{a.status}</td>
                          <td>{a.approvedByName || "—"}</td>
                          <td>{a.actionDate ? new Date(a.actionDate).toLocaleString() : "—"}</td>
                          <td>{a.comments || "—"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="actions">
                  <textarea placeholder="Commentaire (obligatoire si rejet)" value={comment} onChange={(e)=>setComment(e.target.value)} />
                  <div className="btns">
                    <button className="ghost" onClick={() => downloadRequestPdf(detail.request.leaveRequestId)}>📄 Télécharger PDF</button>
                    <button disabled={actionBusy} onClick={() => handleAction("Approve")}>✅ Approuver</button>
                    <button disabled={actionBusy || !comment.trim()} onClick={() => handleAction("Reject")}>❌ Rejeter</button>
                    <button className="ghost" onClick={() => { setDetail(null); setComment(""); }}>Fermer</button>
                  </div>
                </div>
              </>
            )}
          </div>
        )}

        {showPreview && (
          <div style={overlay} onClick={closePreview} role="presentation">
            <div style={modal} onClick={(e) => e.stopPropagation()}>
              <div style={{ display:"flex", justifyContent:"space-between", marginBottom:8 }}>
                <strong>Justificatif</strong>
                <button onClick={closePreview} style={closeBtn}>✖</button>
              </div>
              <div style={{ textAlign:"center" }}>{renderPreview()}</div>
            </div>
          </div>
        )}
      </main>
    </div>
  );
};

export default RHDashboard;

const overlay = { position:"fixed", inset:0, background:"rgba(0,0,0,0.5)", display:"flex", alignItems:"center", justifyContent:"center", zIndex:1000 };
const modal   = { background:"#fff", borderRadius:12, padding:16, maxWidth:"95vw", maxHeight:"90vh", overflow:"auto", boxShadow:"0 10px 30px rgba(0,0,0,0.3)" };
const closeBtn= { background:"transparent", border:"none", fontSize:18, cursor:"pointer" };
