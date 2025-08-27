import React, { useEffect, useState } from "react";
import { apiGet, apiPost } from "./api";
import { useToast } from "../ui/ToastProvider";

export default function AdminAssignBalance() {
  const toast = useToast();

  const [users, setUsers] = useState([]);
  const [userId, setUserId] = useState("");
  const [balance, setBalance] = useState(0);

  useEffect(() => {
    (async () => {
      try {
        const data = await apiGet("/User");
        setUsers(Array.isArray(data) ? data : []);
      } catch {
        toast.error("Impossible de charger les utilisateurs.");
      }
    })();
  }, [toast]);

  const submit = async (e) => {
    e.preventDefault();
    try {
      await apiPost("/LeaveBalance/set", {
        userId: parseInt(userId, 10),
        currentBalance: Number(balance),
      });
      toast.ok("Solde appliqué à tous les types de congé ✅");
      setUserId("");
      setBalance(0);
    } catch (e2) {
      toast.error(e2?.message || "Échec de l’attribution du solde.");
    }
  };

  return (
    <div className="page">
      <h2>Attribuer un solde</h2>
      <div className="card">
        <form onSubmit={submit} className="form">
          <label>Utilisateur
            <select value={userId} onChange={e=>setUserId(e.target.value)} required>
              <option value="">-- Choisir --</option>
              {users.map(u => (
                <option key={u.userId} value={u.userId}>
                  {u.firstName} {u.lastName} ({u.email})
                </option>
              ))}
            </select>
          </label>
          <label>Solde (jours)
            <input type="number" min="0" value={balance}
                   onChange={e=>setBalance(e.target.value)} required/>
          </label>
          <button type="submit">Attribuer</button>
        </form>
      </div>
    </div>
  );
}
