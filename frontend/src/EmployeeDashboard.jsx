import React, { useState, useEffect, useMemo } from 'react';
import './EmployeeDashboard.css';
import logo from './assets/logo.png';
import usercircle from './assets/User.png';
import { useNavigate } from 'react-router-dom';
import { useToast } from './ui/ToastProvider';
import { downloadRequestPdf } from './admin/api';

const SORTS = [
  { value: 'date_desc', label: 'Plus récent' },
  { value: 'date_asc',  label: 'Plus ancien' },
  { value: 'start_asc', label: 'Du ↑' },
  { value: 'start_desc',label: 'Du ↓' },
  { value: 'end_asc',   label: 'Au ↑' },
  { value: 'end_desc',  label: 'Au ↓' },
  { value: 'days_asc',  label: 'Jours ↑' },
  { value: 'days_desc', label: 'Jours ↓' },
  { value: 'status',    label: 'Statut (A→Z)' },
  { value: 'type',      label: 'Type (A→Z)' },
];

const statusOrder = (s) => {
  const map = { 'En attente': 0, 'Approuvée': 1, 'Validée': 1, 'Refusée': 2 };
  return map[s] ?? 3;
};

// True si la demande est approuvée/validée (autorisé à télécharger le PDF)
const isApproved = (status) => {
  const s = (status || '').toLowerCase();
  return s.includes('approuv') || s.includes('valid');
};

const EmployeeDashboard = ({ user, onLogout }) => {
  const navigate = useNavigate();
  const toast = useToast();

  const [leaveBalance, setLeaveBalance] = useState(null);
  const [leaveRequests, setLeaveRequests] = useState([]);
  const [sortBy, setSortBy] = useState('date_desc');

  useEffect(() => {
    const fetchLeaveBalance = async () => {
      try {
        const res = await fetch(`https://localhost:7233/api/LeaveBalance/user/${user.userId}`);
        if (!res.ok) throw new Error('Récupération du solde impossible');
        const raw = await res.json();

        const items = Array.isArray(raw)
          ? raw.map(x => ({
              leaveTypeId: x.leaveTypeId ?? x.LeaveTypeId,
              leaveType:   x.leaveType   ?? x.LeaveType,
              balance:     x.balance     ?? x.Balance
            }))
          : [];

        const annual = items.find(i =>
          (typeof i.leaveType === 'string' && i.leaveType.toLowerCase() === 'congé annuel') ||
          i.leaveTypeId === 2
        );

        setLeaveBalance(annual ? Number(annual.balance) : 0);
      } catch (error) {
        console.error('Erreur récupération solde :', error);
        setLeaveBalance(0);
        toast.error("Impossible de récupérer votre solde de congés.");
      }
    };

    const fetchLeaveRequests = async () => {
      try {
        const res = await fetch(`https://localhost:7233/api/LeaveRequest/user/${user.userId}`);
        if (!res.ok) throw new Error('Erreur récupération demandes');
        const data = await res.json();
        setLeaveRequests(Array.isArray(data) ? data : []);
      } catch (error) {
        console.error('Erreur récupération demandes :', error);
        toast.error("Impossible de récupérer vos demandes récentes.");
      }
    };

    if (user?.userId) {
      fetchLeaveBalance();
      fetchLeaveRequests();
    }
  }, [user.userId, toast]);

  const getStatutEmoji = (statut) => {
    if (statut === 'Validée' || statut === 'Approuvée') return '✅ Approuvée';
    if (statut === 'En attente') return '⏳ En attente';
    if (statut === 'Refusée') return '❌ Refusée';
    return statut;
  };

  const displayDays = (req) => {
    if (typeof req.requestedDays === 'number') {
      if (req.isHalfDay) return 0.5;
      return req.requestedDays;
    }
    const sd = new Date(req.startDate);
    const ed = new Date(req.endDate);
    if (isNaN(sd) || isNaN(ed)) return 0;
    const MS = 24 * 60 * 60 * 1000;
    return Math.max(0, Math.round((ed - sd) / MS) + 1);
  };

  const sortedRequests = useMemo(() => {
    const rows = [...leaveRequests];

    const getCreated = (r) => r.createdAt ? new Date(r.createdAt) : new Date(r.startDate);
    const sd = (r) => new Date(r.startDate);
    const ed = (r) => new Date(r.endDate);
    const days = (r) => Number(displayDays(r));

    rows.sort((a, b) => {
      switch (sortBy) {
        case 'date_asc':  return getCreated(a) - getCreated(b);
        case 'start_asc': return sd(a) - sd(b);
        case 'start_desc':return sd(b) - sd(a);
        case 'end_asc':   return ed(a) - ed(b);
        case 'end_desc':  return ed(b) - ed(a);
        case 'days_asc':  return days(a) - days(b);
        case 'days_desc': return days(b) - days(a);
        case 'status': {
          const ca = statusOrder(a.status);
          const cb = statusOrder(b.status);
          if (ca !== cb) return ca - cb;
          return getCreated(b) - getCreated(a);
        }
        case 'type': {
          const ta = (a.leaveType?.name || '').localeCompare(b.leaveType?.name || undefined, 'fr', { sensitivity: 'base' });
          if (ta !== 0) return ta;
          return getCreated(b) - getCreated(a);
        }
        case 'date_desc':
        default:
          return getCreated(b) - getCreated(a);
      }
    });

    return rows;
  }, [leaveRequests, sortBy]);

  const handleNewRequest = () => navigate('/new-request');

  return (
    <div className="employee-dashboard">
      <aside className="sidebar">
        <img src={logo} alt="logo" className="logo" />
        <ul>
          <li>🏠 Dashboard</li>
          <li onClick={handleNewRequest} style={{ cursor: 'pointer' }}>📅 Nouvelle demande</li>
          
          <li onClick={() => navigate('/settings')} style={{ cursor: 'pointer' }}>⚙️ Paramètres</li>
          <li onClick={onLogout} style={{ cursor: 'pointer' }}>📦 Déconnexion</li>
        </ul>
        <footer className="footer">© 2025 – LeaveManager</footer>
      </aside>

      <main className="main-content">
        <header className="dashboard-header">
          <div></div>
          <div className="user-info">
            <img src={usercircle} alt="user" width={24} height={24} />
            <span>{user.fullName}</span>
          </div>
        </header>

        <h2>Bonjour {user.fullName} 👋</h2>
        <p className="subtitle">Voici un résumé de votre activité.</p>

        <section className="solde-section">
          <h3>Solde Congé restant</h3>
          <p>
            → Solde actuel :{' '}
            <strong>
              {leaveBalance !== null ? `${leaveBalance} jours` : 'Chargement...'}
            </strong>
          </p>
        </section>

        <section className="demandes-section">
          <div className="section-header-row">
            <h3>Mes demandes récentes :</h3>

            <div className="sorter">
              <label htmlFor="sortby" className="sorter-label">Trier par :</label>
              <select
                id="sortby"
                className="sorter-select"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
                aria-label="Trier les demandes"
                title="Trier les demandes"
              >
                {SORTS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
          </div>

          <table>
            <thead>
              <tr>
                <th>Type de congé</th>
                <th>Du</th>
                <th>Au</th>
                <th>Jours</th>
                <th>Statut</th>
                <th>PDF</th>
              </tr>
            </thead>
            <tbody>
              {sortedRequests.map((d, idx) => (
                <tr key={idx}>
                  <td>{d.leaveType?.name}</td>
                  <td>{new Date(d.startDate).toLocaleDateString()}</td>
                  <td>{new Date(d.endDate).toLocaleDateString()}</td>
                  <td>{displayDays(d)}</td>
                  <td>{getStatutEmoji(d.status)}</td>
                  <td>
                    {isApproved(d.status) ? (
                      <button
                        className="ghost"
                        title="Télécharger PDF"
                        onClick={() => downloadRequestPdf(d.leaveRequestId)}
                      >
                        📄
                      </button>
                    ) : (
                      "—"
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <button className="demande-button" onClick={handleNewRequest}>
          Faire une nouvelle demande
        </button>
      </main>

      <style>{`
        .section-header-row{
          display:flex;
          align-items:center;
          justify-content:space-between;
          gap:16px;
          margin-bottom:8px;
        }
        .sorter{
          display:flex;
          align-items:center;
          gap:8px;
        }
        .sorter-label{
          font-size:14px;
          color:#555;
        }
        .sorter-select{
          appearance:none;
          -webkit-appearance:none;
          -moz-appearance:none;
          border:1px solid #e0e0e6;
          border-radius:10px;
          padding:8px 12px;
          background:#fff;
          font-size:14px;
          cursor:pointer;
          transition:box-shadow .15s ease, border-color .15s ease;
        }
        .sorter-select:focus{
          outline:none;
          border-color:#8b5cf6;
          box-shadow:0 0 0 3px rgba(139,92,246,.2);
        }
      `}</style>
    </div>
  );
};

export default EmployeeDashboard;
