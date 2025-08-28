import React, { useEffect, useMemo, useState } from "react";
import "../AdminDashboard.css";
import logo from "../assets/logo.png";
import usercircle from "../assets/User.png";
import { useToast } from "../ui/ToastProvider";
import { useNavigate } from "react-router-dom";

import {
  listPdfTemplates,
  getPdfTemplate,
  createPdfTemplate,
  updatePdfTemplate,
  previewPdfTemplate, // optionnel (si backend expose /PdfTemplate/preview)
} from "../admin/api";

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

export default function AdminPdfTemplate() {
  const toast = useToast();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [templateId, setTemplateId] = useState(null);
  const [name, setName] = useState("");
  const [html, setHtml] = useState(defaultTemplate);
  const [isDefault, setIsDefault] = useState(false);

  const [templates, setTemplates] = useState([]);
  const [preview, setPreview] = useState("");

  // Charger la liste puis charger le premier (ou garder un nouveau brouillon)
  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setLoading(true);
        const list = await listPdfTemplates();
        if (!alive) return;
        setTemplates(list || []);
        if ((list || []).length > 0) {
          const first = list[0];
          const full = await getPdfTemplate(first.pdfTemplateId ?? first.PdfTemplateId ?? first.id);
          if (!alive) return;
          setTemplateId(full.pdfTemplateId ?? full.PdfTemplateId ?? full.id);
          setName(full.name || "");
          setHtml(full.html || defaultTemplate);
          setIsDefault(!!full.isDefault);
        } else {
          // aucun template existant → nouveau
          setTemplateId(null);
          setName("Modèle par défaut");
          setHtml(defaultTemplate);
          setIsDefault(true);
        }
      } catch (e) {
        if (alive) toast.error(e?.message || "Erreur de chargement.");
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => { alive = false; };
  }, [toast]);

  async function handleSave() {
    try {
      setSaving(true);
      const payload = { name, html, isDefault };
      if (templateId) {
        await updatePdfTemplate(templateId, payload); // IMPORTANT: passer l'id
      } else {
        const created = await createPdfTemplate(payload);
        const id = created?.id ?? created?.pdfTemplateId ?? created?.PdfTemplateId;
        if (id) setTemplateId(id);
        // recharger la liste
        const list = await listPdfTemplates();
        setTemplates(list || []);
      }
      toast.ok("Modèle sauvegardé.");
    } catch (e) {
      toast.error(e?.message || "Échec de la sauvegarde du modèle.");
    } finally {
      setSaving(false);
    }
  }

  async function handlePreview() {
    try {
      const res = await previewPdfTemplate({ html, sampleData: {} });
      setPreview(res?.html || html);
    } catch {
      setPreview(html); // fallback client
      toast.info("Prévisualisation simplifiée (fallback).");
    }
  }

  async function handleSelectChange(e) {
    const id = Number(e.target.value) || null;
    if (!id) {
      // Nouveau brouillon
      setTemplateId(null);
      setName("");
      setHtml(defaultTemplate);
      setIsDefault(false);
      setPreview("");
      return;
    }
    try {
      const full = await getPdfTemplate(id);
      setTemplateId(full.pdfTemplateId ?? full.PdfTemplateId ?? full.id);
      setName(full.name || "");
      setHtml(full.html || "");
      setIsDefault(!!full.isDefault);
      setPreview("");
    } catch (e) {
      toast.error(e?.message || "Impossible de charger le modèle sélectionné.");
    }
  }

  const selectValue = useMemo(() => String(templateId || ""), [templateId]);

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
          <li className="active">📄 Modèles PDF</li>
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
            Édite le HTML du modèle. Utilise les placeholders (double accolades) remplacés côté serveur.
          </p>

          {loading ? (
            <div className="card">Chargement…</div>
          ) : (
            <>
              {/* Sélecteur simple des modèles existants + option "Nouveau" */}
              <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
                <label>Modèle :</label>
                <select value={selectValue} onChange={handleSelectChange}>
                  <option value="">— Nouveau —</option>
                  {(templates || []).map((t) => {
                    const id = t.pdfTemplateId ?? t.PdfTemplateId ?? t.id;
                    return (
                      <option key={id} value={id}>
                        {t.name} {t.isDefault ? "• (par défaut)" : ""}
                      </option>
                    );
                  })}
                </select>
                <label style={{ marginLeft: 16 }}>Nom :</label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Nom du modèle"
                  style={{ minWidth: 240 }}
                />
                <label style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
                  <input
                    type="checkbox"
                    checked={isDefault}
                    onChange={(e) => setIsDefault(e.target.checked)}
                  />
                  Modèle par défaut
                </label>
              </div>

              <textarea
                value={html}
                onChange={(e) => setHtml(e.target.value)}
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
                <button
                  onClick={handleSave}
                  disabled={saving}
                  style={{ background: "#7b44c3", color: "#fff", border: "none", borderRadius: 12, padding: "10px 16px" }}
                >
                  💾 Enregistrer
                </button>
                <button
                  className="ghost"
                  onClick={handlePreview}
                  style={{ borderRadius: 12, padding: "10px 16px" }}
                >
                  👁️ Prévisualiser
                </button>
                <button
                  className="danger"
                  onClick={() => { setHtml(defaultTemplate); setPreview(""); }}
                  style={{ borderRadius: 12, padding: "10px 16px" }}
                >
                  ↩️ Modèle par défaut
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
