import React, { useState } from 'react';
import './Login.css';
import logo from './assets/logo.png';
import moon from './assets/moon.png';

function Login({ onLogin }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      const response = await fetch('https://localhost:7233/api/Auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'text/plain',
        },
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        throw new Error('Erreur lors de la connexion');
      }

      const data = await response.json();

      // ✅ Stockage local (persistance)
      localStorage.setItem('token', data.token);
      localStorage.setItem('role', data.role);
      localStorage.setItem('fullName', data.fullName);
      localStorage.setItem('userId', data.userId);
      

      // ✅ Transmettre les données à App
      onLogin({
        token: data.token,
        role: data.role,
        fullName: data.fullName,
        userId: data.userId
      });

    } catch (err) {
      console.error("Erreur connexion :", err);
      setError('Identifiants incorrects ou serveur injoignable');
    }
  };

  return (
    <div className="login-container">
      <div className="login-left">
        <img src={logo} alt="LeaveManager Logo" className="login-logo" />
        <h2 className="welcome-title">Bienvenue !</h2>
        <p className="subtitle">Connectez-vous pour accéder à votre espace</p>

        <form className="login-form" onSubmit={handleSubmit}>
          <label htmlFor="email">Email :</label>
          <input
            type="email"
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="nom@exemple.com"
            required
          />

          <label htmlFor="password">Mot de passe :</label>
          <input
            type="password"
            id="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="**********"
            required
          />

          <button type="submit">Se connecter</button>
        </form>

        {error && <p style={{ color: 'red', textAlign: 'center' }}>{error}</p>}

        <footer className="footer">© 2025 – LeaveManager</footer>
      </div>

      <div className="login-right">
        <img src={moon} alt="Moon Illustration" className="moon-image" />
      </div>
    </div>
  );
}

export default Login;
