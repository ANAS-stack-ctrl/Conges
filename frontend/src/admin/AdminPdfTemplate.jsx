import React, { useEffect, useState } from "react";
import "../AdminDashboard.css"; // réutilise la même grille/aside
import logo from "../assets/logo.png";
import usercircle from "../assets/User.png";
import { useToast } from "../ui/ToastProvider";
import { useNavigate } from "react-router-dom";

/**
 * Editeur du modèle PDF (texte HTML/Handlebars-like)
 * Backend attendu :
 *   GET  /api/PdfTemplate          -> { template: string }
 *   PUT  /api/PdfTemplate          -> { template: string } (sauvegarde)
 *   POST /api/PdfTemplate/preview  -> { html: string } (optionnel pour un rendu prévisualisé)
 */
const API_BASE =
  process.env.REACT_APP_API_URL?.replace(/\/$/, "") || "https://localhost:7233";

export default function AdminPdfTemplate() {
  const [tpl, setTpl] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [preview, setPreview] = useState("");
  const toast = useToast();
  const navigate = useNavigate();

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setLoading(true);
        const res = await fetch(`${API_BASE}/api/PdfTemplate`);
        if (!res.ok) throw new Error("Impossible de charger le modèle PDF.");
        const json = await res.json();
        if (alive) setTpl(json?.template || defaultTemplate);
      } catch (e) {
        if (alive) setTpl(defaultTemplate);
        toast.error(e?.message || "Erreur de chargement du modèle.");
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => { alive = false; };
  }, [toast]);

  async function save() {
    try {
      setSaving(true);
      const res = await fetch(`${API_BASE}/api/PdfTemplate`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ template: tpl }),
      });
      if (!res.ok) throw new Error("Échec de la sauvegarde du modèle.");
      toast.ok("Modèle enregistré ✅");
    } catch (e) {
      toast.error(e?.message || "Erreur de sauvegarde.");
    } finally {
      setSaving(false);
    }
  }

  async function makePreview() {
    // Optionnel : si ton backend a un endpoint de preview.
    try {
      const res = await fetch(`${API_BASE}/api/PdfTemplate/preview`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ template: tpl }),
      });
      if (!res.ok) throw new Error("Impossible de générer la prévisualisation.");
      const json = await res.json();
      setPreview(json?.html || "");
    } catch (e) {
      // fallback : on affiche le template brut
      setPreview(tpl);
      toast.info("Preview simplifiée (fallback).");
    }
  }

  return (
    <div className="admin-dashboard">
      <aside className="sidebar">
        <img src={logo} alt="logo" className="logo" />
        <ul>
          <li onClick={() => navigate("/admin")} style={{ cursor: "pointer" }}>
            📊 Tableau de bord
          </li>
          <li onClick={() => navigate("/admin/users")} style={{ cursor: "pointer" }}>
            👥 Utilisateurs
          </li>
          <li onClick={() => navigate("/admin/leave-types")} style={{ cursor: "pointer" }}>
            🏷️ Types de congé
          </li>
          <li onClick={() => navigate("/admin/blackouts")} style={{ cursor: "pointer" }}>
            🚫 Blackout periods
          </li>
          <li className="active">📄 Modèle PDF</li>
          <li onClick={() => navigate("/settings")} style={{ cursor: "pointer" }}>
            ⚙️ Paramètres
          </li>
          <li onClick={() => navigate(-1)} style={{ cursor: "pointer" }}>
            ⬅️ Retour
          </li>
        </ul>
        <footer className="footer">© 2025 – LeaveManager</footer>
      </aside>

      <main className="main-content">
        <header className="dashboard-header">
          <h2>Modèles PDF</h2>
          <div className="user-info">
            <img src={usercircle} alt="user" />
            <span>Admin</span>
          </div>
        </header>

        <section className="panel" style={{ display: "grid", gap: 12 }}>
          <p>
            Configurez le HTML du document de congé. Vous pouvez utiliser des
            placeholders (double accolades) qui seront remplacés côté serveur.
          </p>
          <ul style={{ margin: 0, paddingLeft: 18 }}>
            <li>
              Exemple place­holders :
              {" "}
              <code>{'{{ request.employee }}'}</code>,{" "}
              <code>{'{{ request.type }}'}</code>,{" "}
              <code>{'{{ request.startDate }}'}</code>,{" "}
              <code>{'{{ request.endDate }}'}</code>,{" "}
              <code>{'{{ request.requestedDays }}'}</code>,{" "}
              <code>{'{{ request.isHalfDay }}'}</code>
            </li>
            <li>
              Historique d’approbation :
              {" "}
              <code>{'{{#each approvals}}'}</code> … <code>{'{{/each}}'}</code> avec
              {" "}
              <code>{'{{ this.level }}'}</code>, <code>{'{{ this.status }}'}</code>,{" "}
              <code>{'{{ this.approvedByName }}'}</code>,{" "}
              <code>{'{{ this.actionDate }}'}</code>,{" "}
              <code>{'{{ this.comments }}'}</code>.
            </li>
            <li>
              Logo appli :
              {" "}
              <code>{'{{ app.logoUrl }}'}</code>
            </li>
          </ul>

          {loading ? (
            <div className="card">Chargement…</div>
          ) : (
            <>
              <textarea
                value={tpl}
                onChange={(e) => setTpl(e.target.value)}
                style={{
                  width: "100%",
                  minHeight: 360,
                  resize: "vertical",
                  fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace",
                  fontSize: 14,
                  borderRadius: 12,
                  border: "1px solid #ddd",
                  padding: 12,
                }}
                spellCheck={false}
              />
              <div className="btns" style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button onClick={save} disabled={saving} style={{ background: "#7b44c3", color: "#fff", border: "none", borderRadius: 12, padding: "10px 16px" }}>
                  💾 Enregistrer
                </button>
                <button className="ghost" onClick={makePreview} style={{ borderRadius: 12, padding: "10px 16px" }}>
                  👁️ Prévisualiser
                </button>
                <button className="danger" onClick={() => setTpl(defaultTemplate)} style={{ borderRadius: 12, padding: "10px 16px" }}>
                  ↩️ Revenir au modèle par défaut
                </button>
              </div>

              {!!preview && (
                <div className="panel" style={{ marginTop: 12 }}>
                  <h4>Aperçu (HTML)</h4>
                  <div
                    style={{ border: "1px solid #eee", borderRadius: 12, padding: 12, background: "#fff" }}
                    dangerouslySetInnerHTML={{ __html: preview }}
                  />
                </div>
              )}
            </>
          )}
        </section>
      </main>
    </div>
  );
}

const defaultTemplate = `
<!doctype html>
<html lang="fr">
<head>
<meta charset="utf-8" />
<title>Demande de congé</title>
<style>
  body { font-family: Arial, sans-serif; color:#222; }
  .wrap { max-width: 800px; margin: 0 auto; }
  header { display:flex; justify-content: space-between; align-items:center; margin-bottom: 24px; }
  .brand { display:flex; align-items:center; gap:12px; }
  .brand img { height: 40px; }
  h1 { font-size: 20px; margin: 0; }
  table { width:100%; border-collapse: collapse; margin-top: 8px; }
  th, td { border: 1px solid #ddd; padding: 8px; text-align:left; }
  .muted { color:#666; font-size:12px; }
</style>
</head>
<body>
  <div class="wrap">
    <header>
      <div class="brand">
        <img src="{{ app.logoUrl }}" alt="logo" />
        <strong>LeaveManager</strong>
      </div>
      <div class="muted">Généré le {{ app.now }}</div>
    </header>

    <h1>Demande de congé</h1>

    <table>
      <tbody>
        <tr><th>Employé</th><td>{{ request.employee }}</td></tr>
        <tr><th>Type</th><td>{{ request.type }}</td></tr>
        <tr><th>Du</th><td>{{ request.startDate }}</td></tr>
        <tr><th>Au</th><td>{{ request.endDate }}</td></tr>
        <tr><th>Jours</th><td>{{ request.requestedDays }}</td></tr>
        <tr><th>Demi-journée</th><td>{{ request.isHalfDay }}</td></tr>
        <tr><th>Statut</th><td>{{ request.status }}</td></tr>
      </tbody>
    </table>

    <h3 style="margin-top:24px;">Historique des validations</h3>
    <table>
      <thead><tr><th>Niveau</th><th>Statut</th><th>Par</th><th>Date</th><th>Commentaires</th></tr></thead>
      <tbody>
        {{#each approvals}}
        <tr>
          <td>{{ this.level }}</td>
          <td>{{ this.status }}</td>
          <td>{{ this.approvedByName }}</td>
          <td>{{ this.actionDate }}</td>
          <td>{{ this.comments }}</td>
        </tr>
        {{/each}}
      </tbody>
    </table>

    <div style="margin-top:24px;">
      <strong>Signature employé :</strong><br/>
      <img src="{{ request.signatureUrl }}" alt="signature" style="max-height:120px;"/>
    </div>
  </div>
</body>
</html>
`.trim();
