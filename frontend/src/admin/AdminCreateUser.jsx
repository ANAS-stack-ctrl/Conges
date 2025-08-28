// src/admin/AdminCreateUser.jsx
import React, { useEffect, useState } from "react";
import { apiPost } from "./api";
import { listHierarchies } from "./api";
import { useToast } from "../ui/ToastProvider";

const ROLES = ["Employee", "Manager", "Director", "RH", "Admin"];

export default function AdminCreateUser() {
  const toast = useToast();

  const [f, setF] = useState({
    firstName: "",
    lastName: "",
    email: "",
    role: "Employee",
  });

  // On stocke la valeur envoyée telle que le backend l’accepte : STRING
  // (ex: "H2", "2" ou "Equipe A"). Chaîne vide => aucune hiérarchie.
  const [hierarchyId, setHierarchyId] = useState("");

  const [hierarchies, setHierarchies] = useState([]);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const hs = await listHierarchies();
        if (!alive) return;
        setHierarchies(Array.isArray(hs) ? hs : []);
      } catch {
        // non bloquant
      }
    })();
    return () => {
      alive = false;
    };
  }, []);

  function isValidEmail(s) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(s).trim());
  }

  async function submit(e) {
    e.preventDefault();

    if (!f.firstName.trim() || !f.lastName.trim()) {
      toast.warn("Veuillez saisir le prénom et le nom.");
      return;
    }
    if (!isValidEmail(f.email)) {
      toast.warn("Veuillez saisir un email valide.");
      return;
    }

    setBusy(true);
    try {
      // ⚠️ IMPORTANT : envoyer une chaîne pour hierarchyId (ou null/empty)
      const payload = {
        firstName: f.firstName.trim(),
        lastName : f.lastName.trim(),
        email    : f.email.trim(),
        role     : (f.role || "Employee").trim(),
        hierarchyId: hierarchyId?.trim?.() ? hierarchyId.trim() : null
      };

      await apiPost("/User", payload);

      toast.ok("Utilisateur créé ✅");
      setF({ firstName: "", lastName: "", email: "", role: "Employee" });
      setHierarchyId("");
    } catch (err) {
      // api.js renvoie déjà un message utile (title/message/error ou texte)
      toast.error(err?.message || "Création impossible.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="page" style={{ maxWidth: 720, margin: "0 auto" }}>
      <h2>Créer un utilisateur</h2>

      <div className="card" style={{ padding: 16 }}>
        <form onSubmit={submit} className="form" style={{ display: "grid", gap: 12 }}>
          <label>
            Prénom
            <input
              value={f.firstName}
              onChange={(e) => setF({ ...f, firstName: e.target.value })}
              required
            />
          </label>

          <label>
            Nom
            <input
              value={f.lastName}
              onChange={(e) => setF({ ...f, lastName: e.target.value })}
              required
            />
          </label>

          <label>
            Email
            <input
              type="email"
              value={f.email}
              onChange={(e) => setF({ ...f, email: e.target.value })}
              required
            />
          </label>

          <label>
            Rôle
            <select
              value={f.role}
              onChange={(e) => setF({ ...f, role: e.target.value })}
            >
              {ROLES.map((r) => (
                <option key={r} value={r}>{r}</option>
              ))}
            </select>
          </label>

          <label>
            Hiérarchie
            <select
              value={hierarchyId}
              onChange={(e) => setHierarchyId(e.target.value)}
              title='L’API accepte "H2", "2" ou le nom exact.'
            >
              <option value="">— Aucune —</option>
              {hierarchies.map((h) => {
                // On construit une valeur STRING tolérée par le backend :
                // on privilégie le Code s’il existe (H1/H2), sinon l’ID en texte,
                // sinon le Nom.
                const value =
                  (h.code && String(h.code)) ||
                  (h.hierarchyId && String(h.hierarchyId)) ||
                  (h.name && String(h.name)) ||
                  "";

                const label = h.name
                  ? h.code
                    ? `${h.name} (${h.code})`
                    : h.name
                  : h.code || value;

                return (
                  <option key={h.hierarchyId || h.code || value} value={value}>
                    {label}
                  </option>
                );
              })}
            </select>
          </label>

          <small style={{ color: "#666" }}>
            L’API accepte une chaîne pour la hiérarchie (ex: <code>"H2"</code> ou <code>"2"</code>).
          </small>

          <button type="submit" disabled={busy}>
            {busy ? "Création..." : "Créer"}
          </button>
        </form>
      </div>
    </div>
  );
}
