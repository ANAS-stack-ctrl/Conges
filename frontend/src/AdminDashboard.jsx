import React, { useEffect, useMemo, useState } from "react";
import "./AdminDashboard.css";
import logo from "./assets/logo.png";
import usercircle from "./assets/User.png";
import { useNavigate } from "react-router-dom";
import { useToast } from "./ui/ToastProvider";

const API_BASE =
  process.env.REACT_APP_API_URL?.replace(/\/$/, "") || "https://localhost:7233";

export default function AdminDashboard({ user, onLogout }) {
  const navigate = useNavigate();
  const toast = useToast();

  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");
  const [users, setUsers] = useState([]);
  const [leaveTypes, setLeaveTypes] = useState([]);

  // ---------- Chargement (toasts uniquement)
  useEffect(() => {
    let alive = true;

    async function load() {
      try {
        setLoading(true);
        setErr("");

        const [uRes, ltRes] = await Promise.all([
          fetch(`${API_BASE}/api/User`),
          fetch(`${API_BASE}/api/LeaveType`),
        ]);

        if (!uRes.ok) throw new Error("Impossible de charger les utilisateurs.");
        if (!ltRes.ok) throw new Error("Impossible de charger les types de congé.");

        const [uJson, ltJson] = await Promise.all([uRes.json(), ltRes.json()]);
        if (!alive) return;

        setUsers(Array.isArray(uJson) ? uJson : []);
        setLeaveTypes(Array.isArray(ltJson) ? ltJson : []);

        toast.ok("Tableau de bord chargé.");
      } catch (e) {
        if (!alive) return;
        const msg = e?.message || "Erreur de chargement.";
        setErr(msg);
        toast.error(msg);
      } finally {
        if (alive) setLoading(false);
      }
    }

    load();
    return () => {
      alive = false;
    };
  }, [toast]);

  // ---------- Statistiques
  const stats = useMemo(() => {
    const utilisateurs = users.length;
    const typesConge = leaveTypes.length;

    const rolesSet = new Set(
      users.map(
        (u) =>
          u.role ??
          u.Role ??
          u.userRole?.role ??
          u.UserRole?.role ??
          "(non défini)"
      )
    );
    const rolesDefinis = rolesSet.size;

    return { utilisateurs, typesConge, rolesDefinis };
  }, [users, leaveTypes]);

  // ---------- Actions rapides (navigation + toast)
  const go = (path, msg) => {
    toast.info(msg);
    navigate(path);
  };

  const actionsRapides = [
    { label: "👥 Gérer les utilisateurs", action: () => go("/admin/users", "Ouverture de la gestion des utilisateurs…") },
    { label: "🏷️ Gérer les types de congé", action: () => go("/admin/leave-types", "Ouverture des types de congé…") },
    { label: "📅 Jours fériés", action: () => go("/admin/holidays", "Ouverture des jours fériés…") },
    { label: "➕ Créer un utilisateur", action: () => go("/admin/create-user", "Création d’un utilisateur…") },
    { label: "💼 Attribuer un solde", action: () => go("/admin/assign-balance", "Attribution de soldes…") },
    { label: "🚫 Blackout periods", action: () => go("/admin/blackouts", "Ouverture des blackout periods…") },
    { label: "📄 Modèle PDF", action: () => go("/admin/pdf-template", "Ouverture du modèle PDF…") },
    { label: "⚙️ Paramètres", action: () => go("/settings", "Ouverture des paramètres…") },
  ];

  return (
    <div className="admin-dashboard">
      {/* ------- Sidebar ------- */}
      <aside className="sidebar">
        <img src={logo} alt="logo" className="logo" />

        <ul>
          <li className="active">📊 Tableau de bord</li>
          <li onClick={actionsRapides[0].action} style={{ cursor: "pointer" }}>
            👥 Utilisateurs
          </li>
          <li onClick={actionsRapides[1].action} style={{ cursor: "pointer" }}>
            🏷️ Types de congés
          </li>
          <li onClick={actionsRapides[2].action} style={{ cursor: "pointer" }}>
            📅 Jours fériés
          </li>
          <li onClick={actionsRapides[5].action} style={{ cursor: "pointer" }}>
            🚫 Blackout periods
          </li>
          <li onClick={actionsRapides[6].action} style={{ cursor: "pointer" }}>
            📄 Modèle PDF
          </li>
          <li onClick={actionsRapides[7].action} style={{ cursor: "pointer" }}>
            ⚙️ Paramètres
          </li>
          <li
            onClick={() => {
              toast.info("Déconnexion…");
              onLogout?.();
            }}
            style={{ cursor: "pointer" }}
          >
            📦 Déconnexion
          </li>
        </ul>

        <footer className="footer">© 2025 – LeaveManager</footer>
      </aside>

      {/* ------- Main ------- */}
      <main className="main-content">
        <header className="dashboard-header">
          <div />
          <div className="user-info">
            <img src={usercircle} alt="user" />
            <span>{user?.fullName || "Administrateur"}</span>
          </div>
        </header>

        <h2>Bonjour {user?.fullName || "Admin"} 👋</h2>
        <p className="subtitle">Vous êtes connecté en tant qu’administrateur.</p>

        {loading && <div className="card" style={{ marginTop: 16 }}>Chargement…</div>}
        {!!err && <div className="card error" style={{ marginTop: 16 }}>{err}</div>}

        {!loading && !err && (
          <>
            {/* --- Cartes stats --- */}
            <section className="stat-cards">
              <div className="card">👥 Utilisateurs : <strong>{stats.utilisateurs}</strong></div>
              <div className="card">🏷️ Types de congé : <strong>{stats.typesConge}</strong></div>
              <div className="card">🔐 Rôles : <strong>{stats.rolesDefinis}</strong></div>
            </section>

            {/* --- Actions rapides --- */}
            <section className="actions-rapides">
              <h3>Actions rapides :</h3>
              <div className="actions-list">
                {actionsRapides.map((a, i) => (
                  <button key={i} onClick={a.action}>{a.label}</button>
                ))}
              </div>
            </section>

            {/* --- Mini listes --- */}
            <section className="mini-lists">
              <div className="panel">
                <h4>Derniers utilisateurs</h4>
                <ul className="list">
                  {users.slice(0, 6).map((u) => (
                    <li key={u.userId || u.UserId}>
                      <span>{u.firstName} {u.lastName}</span>
                      <em>
                        {u.role ??
                          u.Role ??
                          u.userRole?.role ??
                          u.UserRole?.role ??
                          "—"}
                      </em>
                    </li>
                  ))}
                </ul>
                <div className="panel-actions">
                  <button onClick={actionsRapides[0].action}>Voir tout</button>
                </div>
              </div>

              <div className="panel">
                <h4>Types de congé</h4>
                <ul className="list">
                  {leaveTypes.slice(0, 6).map((t) => (
                    <li key={t.leaveTypeId}>
                      <span>{t.name}</span>
                      <em>{t.consecutiveDays > 0 ? `${t.consecutiveDays} j consécutifs` : "illimité"}</em>
                    </li>
                  ))}
                </ul>
                <div className="panel-actions">
                  <button onClick={actionsRapides[1].action}>Voir tout</button>
                </div>
              </div>
            </section>
          </>
        )}
      </main>
    </div>
  );
}
