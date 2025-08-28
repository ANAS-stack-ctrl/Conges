// src/admin/api.js
const API_BASE = "https://localhost:7233/api";
// Utile pour construire des URLs de fichiers (uploads, logo, etc.)
const FILE_BASE = API_BASE.replace(/\/api$/, "");

// ─────────────────────────────────────────────────────────────
// Core fetch helper
// ─────────────────────────────────────────────────────────────
async function request(path, opts = {}) {
  const res = await fetch(`${API_BASE}${path}`, opts);
  if (!res.ok) {
    let msg = `${res.status} ${res.statusText}`;
    try {
      const ct = res.headers.get("content-type") || "";
      if (ct.includes("application/json")) {
        const j = await res.json();
        msg = j?.title || j?.message || j?.error || msg;
      } else {
        const t = await res.text();
        if (t) msg = t;
      }
    } catch {}
    throw new Error(msg);
  }
  if (res.status === 204) return {};
  const ct = res.headers.get("content-type") || "";
  return ct.includes("application/json") ? res.json() : {};
}

// ─────────────────────────────────────────────────────────────
export function apiGet(path) {
  return request(path);
}
export function apiPost(path, body) {
  return request(path, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body ?? {}),
  });
}
export function apiPut(path, body) {
  return request(path, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body ?? {}),
  });
}
export function apiDelete(path) {
  return request(path, { method: "DELETE" });
}

// ─────────────────────────────────────────────────────────────
// Normalisations utiles
// ─────────────────────────────────────────────────────────────
function bool(v) {
  if (typeof v === "boolean") return v;
  if (typeof v === "string") return v === "true" || v === "1" || v === "on";
  return !!v;
}
function num(v, def = 0) {
  const n = Number(v);
  return Number.isFinite(n) ? n : def;
}
function normLeaveTypePayload(p = {}) {
  const policyId =
    p.policyId === null || p.policyId === undefined || p.policyId === ""
      ? null
      : num(p.policyId);

  return {
    name: (p.name || "").trim(),
    requiresProof: bool(p.requiresProof),
    consecutiveDays: num(p.consecutiveDays, 0),
    approvalFlow: (p.approvalFlow || "").trim(), // "Serial" | "Parallel"
    policyId,
  };
}

// ─────────────────────────────────────────────────────────────
// Users
// ─────────────────────────────────────────────────────────────
export const listUsers = () => apiGet("/User");
export const createUser = (payload) => apiPost("/User", payload);
export const deleteUser = (id) => apiDelete(`/User/${id}`);
export const changeRole = (user) => apiPut(`/User/${user.userId}`, user);

// ─────────────────────────────────────────────────────────────
// Leave types
// ─────────────────────────────────────────────────────────────
export const listTypes = () => apiGet("/LeaveType");
export const createType = (payload) =>
  apiPost("/LeaveType", normLeaveTypePayload(payload));
export const updateType = (id, payload) =>
  apiPut(`/LeaveType/${id}`, normLeaveTypePayload(payload));
export const deleteType = (id) => apiDelete(`/LeaveType/${id}`);

// ─────────────────────────────────────────────────────────────
// Leave balance
// ─────────────────────────────────────────────────────────────
export const setBalanceAllTypes = (userId, currentBalance) =>
  apiPost("/LeaveBalance/set", { userId, currentBalance });

// ─────────────────────────────────────────────────────────────
// Approvals (Manager / Director / RH)
// ─────────────────────────────────────────────────────────────
// ⬇️ MAJ : accepte { role, reviewerUserId } (et garde userId en alias)
export function getPendingApprovals(arg) {
  let role, reviewerUserId, userId, hierarchyId;
  if (typeof arg === "string") {
    role = arg;
  } else if (arg && typeof arg === "object") {
    ({ role, reviewerUserId, userId, hierarchyId } = arg);
  }

  const qs = new URLSearchParams();
  if (role) qs.set("role", role);
  if (reviewerUserId) {
    qs.set("reviewerUserId", String(reviewerUserId));
  } else if (userId) {
    // compat (ancien backend)
    qs.set("userId", String(userId));
  }
  if (hierarchyId) qs.set("hierarchyId", String(hierarchyId));

  const suffix = qs.toString() ? `?${qs.toString()}` : "";
  return apiGet(`/Approval/pending${suffix}`);
}

export function actOnApproval(payload) {
  const {
    requestId,
    approvalId,
    action,
    comment,
    comments,
    actorUserId,
    role,
  } = payload || {};

  const body = {
    action,
    comments: comment ?? comments ?? "",
    actorUserId,   // IMPORTANT
    role,          // optionnel
  };

  if (requestId) return apiPost(`/Approval/by-request/${requestId}/action`, body);
  if (approvalId) return apiPost(`/Approval/${approvalId}/action`, body);
  throw new Error("actOnApproval: requestId ou approvalId est requis.");
}

// ─────────────────────────────────────────────────────────────
// RH (liste + stats globales)
// ─────────────────────────────────────────────────────────────
export function rhListRequests(params = {}) {
  const { status, typeId, from, to } = params;
  const qs = new URLSearchParams();
  if (status) qs.set("status", status);
  if (typeId) qs.set("typeId", String(typeId));
  if (from) qs.set("from", from);
  if (to) qs.set("to", to);
  const suffix = qs.toString() ? `?${qs.toString()}` : "";
  return apiGet(`/LeaveRequest/admin/requests${suffix}`);
}
export function rhStats() {
  return apiGet(`/Approval/rh/stats`);
}

// ─────────────────────────────────────────────────────────────
// Stats par rôle — "aujourd'hui"
// ─────────────────────────────────────────────────────────────
export function getRoleStats(role, date) {
  const qs = new URLSearchParams();
  qs.set("role", role);
  if (date) qs.set("date", date);
  return apiGet(`/Approval/stats/role?${qs.toString()}`);
}

// ─────────────────────────────────────────────────────────────
// Historique & PDF
// ─────────────────────────────────────────────────────────────
export const getApprovalHistory = (requestId) =>
  apiGet(`/Approval/history/${requestId}`);

// ► Téléchargement du PDF d’une demande
export const downloadRequestPdf = (requestId, templateId) => {
  const qs = templateId ? `?templateId=${templateId}` : "";
  return fetch(`${API_BASE}/Export/leave-request/${requestId}${qs}`, {
    method: "GET",
  }).then(async (res) => {
    if (!res.ok) throw new Error(await res.text());
    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `Demande_${requestId}.pdf`;
    a.click();
    window.URL.revokeObjectURL(url);
    return true;
  });
};

// ─────────────────────────────────────────────────────────────
// Blackout periods
// ─────────────────────────────────────────────────────────────
export function listBlackoutsAdmin({ active, scope, from, to, page, pageSize } = {}) {
  const qs = new URLSearchParams();
  if (active !== undefined) qs.set("active", String(active));
  if (scope) qs.set("scope", scope);
  if (from) qs.set("from", from);
  if (to) qs.set("to", to);
  if (page) qs.set("page", String(page));
  if (pageSize) qs.set("pageSize", String(pageSize));
  const suffix = qs.toString() ? `?${qs.toString()}` : "";
  return apiGet(`/Blackout/admin${suffix}`);
}
export function createBlackout(payload) { return apiPost("/Blackout/admin", payload); }
export function updateBlackout(id, payload) { return apiPut(`/Blackout/admin/${id}`, payload); }
export function deleteBlackout(id) { return apiDelete(`/Blackout/admin/${id}`); }
export function getActiveBlackouts({ from, to, userId, leaveTypeId, departmentId }) {
  const qs = new URLSearchParams();
  if (from) qs.set("from", from);
  if (to) qs.set("to", to);
  if (userId) qs.set("userId", String(userId));
  if (leaveTypeId) qs.set("leaveTypeId", String(leaveTypeId));
  if (departmentId) qs.set("departmentId", String(departmentId));
  return apiGet(`/Blackout/active?${qs.toString()}`);
}

// ─────────────────────────────────────────────────────────────
// Templates PDF (Admin)
// ─────────────────────────────────────────────────────────────
export const listPdfTemplates   = () => apiGet("/PdfTemplate");
export const getPdfTemplate     = (id) => apiGet(`/PdfTemplate/${id}`);
export const createPdfTemplate  = (tpl) => apiPost("/PdfTemplate", tpl);
export const updatePdfTemplate  = (id, tpl) => apiPut(`/PdfTemplate/${id}`, tpl);
export const deletePdfTemplate  = (id) => apiDelete(`/PdfTemplate/${id}`);

// ─────────────────────────────────────────────────────────────
// Hierarchies (Equipes / Départements) — CRUD + membres
// ─────────────────────────────────────────────────────────────
export function listHierarchies() {
  return apiGet("/Hierarchy");
}
export function createHierarchy(payload) {
  // { name, code?, description? }
  return apiPost("/Hierarchy", payload);
}
export function updateHierarchy(id, payload) {
  return apiPut(`/Hierarchy/${id}`, payload);
}
export function deleteHierarchy(id) {
  return apiDelete(`/Hierarchy/${id}`);
}

// Membres d'une hiérarchie
export function getHierarchyMembers(id) {
  return apiGet(`/Hierarchy/${id}/members`);
}
export function addHierarchyMember(id, payload) {
  // { userId, role: "Employee" | "Manager" | "Director" | "RH" }
  return apiPost(`/Hierarchy/${id}/members`, payload);
}
export function removeHierarchyMember(id, userId) {
  return apiDelete(`/Hierarchy/${id}/members/${userId}`);
}

// ► Optionnel : candidats (utilisateurs non-membres)
export function getHierarchyCandidates(id, role) {
  const qs = role ? `?role=${encodeURIComponent(role)}` : "";
  return apiGet(`/Hierarchy/${id}/candidates${qs}`);
}

// ─────────────────────────────────────────────────────────────
// exports utilitaires
// ─────────────────────────────────────────────────────────────
export { API_BASE, FILE_BASE };
