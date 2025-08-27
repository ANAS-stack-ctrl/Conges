import React, { useEffect, useMemo, useState } from "react";
import {
  listBlackoutsAdmin,
  createBlackout,
  updateBlackout,
  deleteBlackout,
} from "./api";
import "./BlackoutAdmin.css";
import { useToast } from "../ui/ToastProvider";
import { useConfirm } from "../ui/ConfirmProvider";

const SCOPE_OPTIONS = [
  { value: "Global", label: "Global" },
  { value: "LeaveType", label: "Par type de congé" },
  { value: "Department", label: "Par département" },
  { value: "User", label: "Par utilisateur" },
];

const ENFORCE_OPTIONS = [
  { value: "Warn", label: "Avertir" },
  { value: "Block", label: "Bloquer" },
];

function todayISO(d = new Date()) {
  const pad = (n) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

const emptyForm = {
  name: "",
  startDate: todayISO(),
  endDate: todayISO(),
  scopeType: "Global",
  leaveTypeId: "",
  departmentId: "",
  userId: "",
  enforceMode: "Warn",
  reason: "",
  isActive: true,
};

export default function BlackoutAdmin() {
  const toast = useToast();
  const confirm = useConfirm().confirm;

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  // filtres
  const [activeFilter, setActiveFilter] = useState("all");
  const [scopeFilter, setScopeFilter] = useState("all");
  const [fromFilter, setFromFilter] = useState("");
  const [toFilter, setToFilter] = useState("");

  // pagination
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);

  // modale
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);

  const qs = useMemo(() => {
    const q = { page, pageSize };
    if (activeFilter === "true") q.active = true;
    if (activeFilter === "false") q.active = false;
    if (scopeFilter !== "all") q.scope = scopeFilter;
    if (fromFilter) q.from = fromFilter;
    if (toFilter) q.to = toFilter;
    return q;
  }, [page, pageSize, activeFilter, scopeFilter, fromFilter, toFilter]);

  async function load() {
    setLoading(true);
    setErr("");
    try {
      const data = await listBlackoutsAdmin(qs);
      setItems(Array.isArray(data) ? data : data?.items || []);
    } catch (e) {
      const msg = e?.message || "Impossible de charger les blackout periods.";
      setErr(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [qs.page, qs.pageSize, qs.active, qs.scope, qs.from, qs.to]);

  function newBlackout() {
    setEditingId(null);
    setForm(emptyForm);
    setOpen(true);
  }

  function editBlackout(row) {
    setEditingId(row.blackoutPeriodId);
    setForm({
      name: row.name || "",
      startDate: row.startDate ? row.startDate.substring(0, 10) : todayISO(),
      endDate: row.endDate ? row.endDate.substring(0, 10) : todayISO(),
      scopeType: row.scopeType || "Global",
      leaveTypeId: row.leaveTypeId ?? "",
      departmentId: row.departmentId ?? "",
      userId: row.userId ?? "",
      enforceMode: row.enforceMode || "Warn",
      reason: row.reason || "",
      isActive: !!row.isActive,
    });
    setOpen(true);
  }

  async function removeBlackout(id) {
    const ok = await confirm({
      title: "Supprimer",
      message: "Supprimer ce blackout period ?",
      okText: "Supprimer",
      variant: "danger",
    });
    if (!ok) return;

    try {
      await deleteBlackout(id);
      toast.ok("Blackout supprimé.");
      await load();
    } catch (e) {
      toast.error(e?.message || "Suppression impossible.");
    }
  }

  function onChange(e) {
    const { name, value, type, checked } = e.target;
    setForm((f) => ({ ...f, [name]: type === "checkbox" ? checked : value }));
  }

  function validate() {
    if (!form.name.trim()) return "Le nom est requis.";
    if (!form.startDate || !form.endDate) return "Dates requises.";
    if (new Date(form.endDate) < new Date(form.startDate))
      return "La date de fin ne peut pas être avant la date de début.";
    if (form.scopeType === "LeaveType" && !form.leaveTypeId)
      return "leaveTypeId requis pour le scope 'LeaveType'.";
    if (form.scopeType === "Department" && !form.departmentId)
      return "departmentId requis pour le scope 'Department'.";
    if (form.scopeType === "User" && !form.userId)
      return "userId requis pour le scope 'User'.";
    return "";
  }

  async function save(e) {
    e.preventDefault();
    const v = validate();
    if (v) {
      toast.warn(v);
      return;
    }
    setSaving(true);
    try {
      const payload = {
        name: form.name.trim(),
        startDate: new Date(form.startDate),
        endDate: new Date(form.endDate),
        scopeType: form.scopeType,
        leaveTypeId: form.scopeType === "LeaveType" ? Number(form.leaveTypeId) : null,
        departmentId: form.scopeType === "Department" ? Number(form.departmentId) : null,
        userId: form.scopeType === "User" ? Number(form.userId) : null,
        enforceMode: form.enforceMode,
        reason: form.reason?.trim() ?? "",
        isActive: !!form.isActive,
      };

      if (editingId) {
        await updateBlackout(editingId, payload);
        toast.ok("Blackout mis à jour.");
      } else {
        await createBlackout(payload);
        toast.ok("Blackout créé.");
      }
      setOpen(false);
      setEditingId(null);
      setForm(emptyForm);
      await load();
    } catch (e) {
      toast.error(e?.message || "Échec de l’enregistrement.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="blackout-admin">
      <header className="bo-header">
        <h2>Blackout periods</h2>
        <div className="bo-actions">
          <button onClick={newBlackout}>➕ Nouveau</button>
        </div>
      </header>

      {/* Filtres */}
      <section className="bo-filters">
        <label>
          Actif :
          <select
            value={activeFilter}
            onChange={(e) => { setPage(1); setActiveFilter(e.target.value); }}
          >
            <option value="all">Tous</option>
            <option value="true">Actifs</option>
            <option value="false">Inactifs</option>
          </select>
        </label>

        <label>
          Scope :
          <select
            value={scopeFilter}
            onChange={(e) => { setPage(1); setScopeFilter(e.target.value); }}
          >
            <option value="all">Tous</option>
            {SCOPE_OPTIONS.map((s) => (
              <option key={s.value} value={s.value}>{s.label}</option>
            ))}
          </select>
        </label>

        <label>
          Du :
          <input
            type="date"
            value={fromFilter}
            onChange={(e) => { setPage(1); setFromFilter(e.target.value); }}
          />
        </label>

        <label>
          Au :
          <input
            type="date"
            value={toFilter}
            onChange={(e) => { setPage(1); setToFilter(e.target.value); }}
          />
        </label>

        <button className="ghost" onClick={() => {
          setActiveFilter("all");
          setScopeFilter("all");
          setFromFilter("");
          setToFilter("");
          setPage(1);
        }}>Réinitialiser</button>
      </section>

      {loading ? (
        <div className="card">Chargement…</div>
      ) : err ? (
        <div className="card error">{err}</div>
      ) : (
        <>
          <div className="table-wrap">
            <table className="bo-table">
              <thead>
                <tr>
                  <th>Nom</th>
                  <th>Du</th>
                  <th>Au</th>
                  <th>Scope</th>
                  <th>Clé</th>
                  <th>Mode</th>
                  <th>Actif</th>
                  <th>Raison</th>
                  <th style={{ width: 140 }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 ? (
                  <tr>
                    <td colSpan={9} style={{ textAlign: "center" }}>
                      Aucun blackout period
                    </td>
                  </tr>
                ) : (
                  items.map((b) => (
                    <tr key={b.blackoutPeriodId}>
                      <td>{b.name}</td>
                      <td>{new Date(b.startDate).toLocaleDateString()}</td>
                      <td>{new Date(b.endDate).toLocaleDateString()}</td>
                      <td>{b.scopeType}</td>
                      <td>
                        {b.scopeType === "LeaveType" && b.leaveTypeId != null
                          ? `TypeId: ${b.leaveTypeId}`
                          : b.scopeType === "Department" && b.departmentId != null
                          ? `DeptId: ${b.departmentId}`
                          : b.scopeType === "User" && b.userId != null
                          ? `UserId: ${b.userId}`
                          : "—"}
                      </td>
                      <td>{b.enforceMode}</td>
                      <td>{b.isActive ? "✅" : "—"}</td>
                      <td className="reason-cell" title={b.reason || ""}>
                        {b.reason || "—"}
                      </td>
                      <td className="row-actions">
                        <button onClick={() => editBlackout(b)}>✏️</button>
                        <button className="danger" onClick={() => removeBlackout(b.blackoutPeriodId)}>🗑️</button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          <div className="bo-pager">
            <button disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>◀︎</button>
            <span>Page {page}</span>
            <button disabled={items.length < pageSize} onClick={() => setPage((p) => p + 1)}>▶︎</button>
          </div>
        </>
      )}

      {open && (
        <div className="modal-backdrop" onClick={() => !saving && setOpen(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3>{editingId ? "Modifier un blackout" : "Nouveau blackout"}</h3>
            <form onSubmit={save} className="form-grid">
              <label>Nom
                <input name="name" value={form.name} onChange={onChange} required />
              </label>

              <label>Du
                <input type="date" name="startDate" value={form.startDate} onChange={onChange} required />
              </label>

              <label>Au
                <input type="date" name="endDate" value={form.endDate} onChange={onChange} required />
              </label>

              <label>Scope
                <select name="scopeType" value={form.scopeType} onChange={onChange}>
                  {SCOPE_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </label>

              {form.scopeType === "LeaveType" && (
                <label>LeaveTypeId
                  <input name="leaveTypeId" value={form.leaveTypeId} onChange={onChange} type="number" min="1" required />
                </label>
              )}

              {form.scopeType === "Department" && (
                <label>DepartmentId
                  <input name="departmentId" value={form.departmentId} onChange={onChange} type="number" min="1" required />
                </label>
              )}

              {form.scopeType === "User" && (
                <label>UserId
                  <input name="userId" value={form.userId} onChange={onChange} type="number" min="1" required />
                </label>
              )}

              <label>Mode
                <select name="enforceMode" value={form.enforceMode} onChange={onChange}>
                  {ENFORCE_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
              </label>

              <label className="full">Raison (facultatif)
                <textarea name="reason" value={form.reason} onChange={onChange} rows={3} />
              </label>

              <label className="checkbox">
                <input type="checkbox" name="isActive" checked={form.isActive} onChange={onChange} />
                Actif
              </label>

              <div className="modal-actions">
                <button type="button" className="ghost" disabled={saving} onClick={() => setOpen(false)}>Annuler</button>
                <button disabled={saving} type="submit">{saving ? "Enregistrement..." : "Enregistrer"}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
