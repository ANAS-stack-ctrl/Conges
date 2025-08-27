 import React, { useState } from 'react';
import './SettingsPage.css';
import logo from './assets/logo.png';
import userIcon from './assets/User.png';

const SettingsPage = ({ user, onLogout }) => {
  const [language, setLanguage] = useState('fr');
  const [form, setForm] = useState({
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  });

  const handlePasswordChange = (e) => {
    e.preventDefault();
    // Logique de mise à jour du mot de passe ici
    console.log('Mot de passe mis à jour', form);
  };

  const handleSave = () => {
    // Logique d’enregistrement des préférences
    console.log('Préférences enregistrées');
  };

  return (
    <div className="settings-page">
      <aside className="sidebar">
        <img src={logo} alt="logo" className="logo" />
        <ul>
          <li>🏠 Dashboard</li>
          <li className="active">⚙️ Paramètres</li>
          <li onClick={onLogout} style={{ cursor: 'pointer' }}>📦 Déconnexion</li>
        </ul>
        <footer className="footer">© 2025 – LeaveManager</footer>
      </aside>

      <main className="settings-content">
        <header className="dashboard-header">
          <div></div>
          <div className="user-info">
            <img src={userIcon} alt="user" />
            <span>{user.fullName}</span>
          </div>
        </header>

        <h2>⚙️ Paramètres du compte</h2>

        <section className="settings-section">
          <h3>👤 Informations personnelles</h3>
          <p>Nom : <strong>{user.lastName}</strong></p>
          <p>Prénom : <strong>{user.firstName}</strong></p>
          <p>Email : <strong>{user.email}</strong></p>
          <p>Fonction : <strong>{user.role}</strong></p>
          <button className="btn-outline">✏️ Modifier les infos</button>
        </section>

        <section className="settings-section">
          <h3>🔒 Changer le mot de passe</h3>
          <form onSubmit={handlePasswordChange}>
            <label>Ancien mot de passe :</label>
            <input type="password" required onChange={e => setForm({ ...form, oldPassword: e.target.value })} />

            <label>Nouveau mot de passe :</label>
            <input type="password" required onChange={e => setForm({ ...form, newPassword: e.target.value })} />

            <label>Confirmer mot de passe :</label>
            <input type="password" required onChange={e => setForm({ ...form, confirmPassword: e.target.value })} />

            <button type="submit" className="btn-purple">🔁 Mettre à jour le mot de passe</button>
          </form>
        </section>

        <section className="settings-section">
          <h3>🌐 Langue de l’interface</h3>
          <label>Sélectionner :</label>
          <select value={language} onChange={e => setLanguage(e.target.value)}>
            <option value="fr">Français</option>
            <option value="en">Anglais</option>
          </select>
        </section>

        <button onClick={handleSave} className="btn-purple">💾 Enregistrer</button>
      </main>
    </div>
  );
};

export default SettingsPage;
