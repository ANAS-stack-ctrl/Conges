import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import "./SettingsPage.css";
import logo from "./assets/logo.png";
import userIcon from "./assets/User.png";

const SettingsPage = ({ user, onLogout }) => {
  const navigate = useNavigate();

  const [language, setLanguage] = useState("fr");
  const [form, setForm] = useState({
    oldPassword: "",
    newPassword: "",
    confirmPassword: "",
  });

  // Route dashboard selon le rôle (adapte si tes routes diffèrent)
  const goToDashboard = () => {
    const role = (user?.role || "").toLowerCase();
    if (role === "admin") navigate("/admin");
    else if (role === "director" || role === "directeur") navigate("/director");
    else if (role === "manager") navigate("/manager");
    else if (role === "rh" || role === "hr") navigate("/rh");
    else navigate("/"); // défaut
  };

  const handleLogout = () => {
    try {
      if (typeof onLogout === "function") onLogout();
      else navigate("/login");
    } catch {
      navigate("/login");
    }
  };

  const handlePasswordChange = (e) => {
    e.preventDefault();
    // TODO: appelle ton endpoint de changement de mot de passe ici
    console.log("Mot de passe mis à jour", form);
  };

  const handleSave = () => {
    // TODO: persister la langue (localStorage, backend, etc.)
    console.log("Préférences enregistrées", { language });
  };

  return (
    <div className="settings-page">
      <aside className="sidebar">
        <img src={logo} alt="logo" className="logo" />
        <nav>
          <button type="button" className="side-item" onClick={goToDashboard}>
            🏠 Dashboard
          </button>
          <button type="button" className="side-item active">
            ⚙️ Paramètres
          </button>
          <button
            type="button"
            className="side-item danger"
            onClick={handleLogout}
          >
            📦 Déconnexion
          </button>
        </nav>
        <footer className="footer">© 2025 – LeaveManager</footer>
      </aside>

      <main className="settings-content">
        <header className="dashboard-header">
          <div />
          <div className="user-info">
            <img src={userIcon} alt="user" />
            <span>{user?.fullName || `${user?.firstName || ""} ${user?.lastName || ""}`.trim()}</span>
          </div>
        </header>

        <h2>⚙️ Paramètres du compte</h2>

        <section className="settings-section">
          <h3>👤 Informations personnelles</h3>
          <p>Nom : <strong>{user?.lastName}</strong></p>
          <p>Prénom : <strong>{user?.firstName}</strong></p>
          <p>Email : <strong>{user?.email}</strong></p>
          <p>Fonction : <strong>{user?.role}</strong></p>
          <button type="button" className="btn-outline">✏️ Modifier les infos</button>
        </section>

        <section className="settings-section">
          <h3>🔒 Changer le mot de passe</h3>
          <form onSubmit={handlePasswordChange}>
            <label>Ancien mot de passe :</label>
            <input
              type="password"
              required
              value={form.oldPassword}
              onChange={(e) => setForm({ ...form, oldPassword: e.target.value })}
            />

            <label>Nouveau mot de passe :</label>
            <input
              type="password"
              required
              value={form.newPassword}
              onChange={(e) => setForm({ ...form, newPassword: e.target.value })}
            />

            <label>Confirmer mot de passe :</label>
            <input
              type="password"
              required
              value={form.confirmPassword}
              onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
            />

            <button type="submit" className="btn-purple">
              🔁 Mettre à jour le mot de passe
            </button>
          </form>
        </section>

        <section className="settings-section">
          <h3>🌐 Langue de l’interface</h3>
          <label>Sélectionner :</label>
          <select value={language} onChange={(e) => setLanguage(e.target.value)}>
            <option value="fr">Français</option>
            <option value="en">Anglais</option>
          </select>
        </section>

        <button type="button" onClick={handleSave} className="btn-purple">
          💾 Enregistrer
        </button>
      </main>
    </div>
  );
};

export default SettingsPage;
