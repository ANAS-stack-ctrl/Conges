import React, { useState } from "react";
import { apiPost } from "./api";
import { useToast } from "../ui/ToastProvider";

const ROLES = ["Employee", "Admin", "RH", "Manager", "Director"];

export default function AdminCreateUser() {
  const toast = useToast();
  const [f, setF] = useState({
    firstName: "", lastName: "", email: "", role: "Employee"
  });

  const submit = async (e) => {
    e.preventDefault();
    try {
      await apiPost("/User", {
        ...f,
        isActive: true,
        passwordHash: "", // ignoré côté backend
      });
      toast.ok("Utilisateur créé ✅");
      setF({ firstName:"", lastName:"", email:"", role:"Employee" });
    } catch (e2) {
      toast.error(e2?.message || "Création impossible.");
    }
  };

  return (
    <div className="page">
      <h2>Créer un utilisateur</h2>
      <div className="card">
        <form onSubmit={submit} className="form">
          <label>Prénom
            <input value={f.firstName} onChange={e=>setF({...f, firstName:e.target.value})} required/>
          </label>
          <label>Nom
            <input value={f.lastName} onChange={e=>setF({...f, lastName:e.target.value})} required/>
          </label>
          <label>Email
            <input type="email" value={f.email} onChange={e=>setF({...f, email:e.target.value})} required/>
          </label>
          <label>Rôle
            <select value={f.role} onChange={e=>setF({...f, role:e.target.value})}>
              {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
            </select>
          </label>
          <button type="submit">Créer</button>
        </form>
      </div>
    </div>
  );
}
