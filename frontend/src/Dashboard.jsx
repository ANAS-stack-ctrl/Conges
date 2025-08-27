import React from 'react';

const Dashboard = ({ user }) => {
  return (
    <div>
      <h1>Bienvenue {user.email || 'utilisateur'} !</h1>
      <p>Vous êtes connecté avec succès.</p>
    </div>
  );
};

export default Dashboard;
