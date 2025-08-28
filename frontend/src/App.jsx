import React, { useState, useEffect } from "react";
import { Routes, Route, Navigate } from "react-router-dom";

import Login from "./Login.jsx";
import AdminDashboard from "./AdminDashboard.jsx";
import EmployeeDashboard from "./EmployeeDashboard.jsx";
import RHDashboard from "./RHDashboard.jsx";
import NewLeaveRequest from "./NewLeaveRequest.jsx";
import SettingsPage from "./SettingsPage.jsx";
import AdminHolidays from "./admin/AdminHolidays.jsx";
import AdminUsers from "./admin/AdminUsers.jsx";
import AdminLeaveTypes from "./admin/AdminLeaveTypes.jsx";
import AdminCreateUser from "./admin/AdminCreateUser.jsx";
import AdminAssignBalance from "./admin/AdminAssignBalance.jsx";
import BlackoutAdmin from "./admin/BlackoutAdmin";
import ManagerDashboard from "./ManagerDashboard";
import DirectorDashboard from "./DirectorDashboard";
import AdminHierarchies from "./admin/AdminHierarchies";
// ➕ éditeur de modèle PDF (nouveau)
import AdminPdfTemplate from "./admin/AdminPdfTemplate.jsx";

// Providers
import ToastProvider from "./ui/ToastProvider";
import ConfirmProvider from "./ui/ConfirmProvider";

function App() {
  const [user, setUser] = useState(null);

  useEffect(() => {
    const token = localStorage.getItem("token");
    const role = localStorage.getItem("role");
    const fullName = localStorage.getItem("fullName");
    const userId = localStorage.getItem("userId");
    const leaveBalance = localStorage.getItem("leaveBalance");

    if (token && role && fullName && userId) {
      setUser({
        token,
        role,
        fullName,
        userId: parseInt(userId, 10),
        leaveBalance,
      });
    }
  }, []);

  const handleLogin = (userData) => {
    setUser(userData);
    localStorage.setItem("token", userData.token);
    localStorage.setItem("role", userData.role);
    localStorage.setItem("fullName", userData.fullName);
    localStorage.setItem("userId", userData.userId);
    if (userData.leaveBalance !== undefined) {
      localStorage.setItem("leaveBalance", userData.leaveBalance);
    }
  };

  const handleLogout = () => {
    localStorage.clear();
    setUser(null);
  };

  const HomeRedirect = () => {
    const r = (user?.role || "").toLowerCase();
    if (r === "admin") return <Navigate to="/admin" />;
    if (r === "rh") return <Navigate to="/rh" />;
    if (r === "manager") return <Navigate to="/manager" />;
    if (r === "director" || r === "directeur") return <Navigate to="/director" />;
    return <Navigate to="/employee" />;
  };

  return (
    <ToastProvider>
      <ConfirmProvider>
        <Routes>
          {!user ? (
            <>
              <Route path="/" element={<Login onLogin={handleLogin} />} />
              <Route path="*" element={<Navigate to="/" />} />
            </>
          ) : (
            <>
              <Route path="/" element={<HomeRedirect />} />

              {/* Dashboards */}
              <Route path="/admin" element={<AdminDashboard user={user} onLogout={handleLogout} />} />
              <Route path="/rh" element={<RHDashboard user={user} onLogout={handleLogout} />} />
              <Route path="/employee" element={<EmployeeDashboard user={user} onLogout={handleLogout} />} />
              <Route path="/manager" element={<ManagerDashboard user={user} onLogout={handleLogout} />} />
              <Route path="/director" element={<DirectorDashboard user={user} onLogout={handleLogout} />} />

              {/* Admin sous-pages */}
              <Route path="/admin/users" element={<AdminUsers />} />
              <Route path="/admin/leave-types" element={<AdminLeaveTypes />} />
              <Route path="/admin/create-user" element={<AdminCreateUser />} />
              <Route path="/admin/assign-balance" element={<AdminAssignBalance />} />
              <Route path="/admin/blackouts" element={<BlackoutAdmin />} />
              {/* ➕ éditeur de template PDF */}
              <Route path="/admin/pdf-template" element={<AdminPdfTemplate />} />
              <Route path="/admin/holidays" element={<AdminHolidays user={user} onLogout={handleLogout} />} />
              {/* Commun */}
              <Route path="/settings" element={<SettingsPage user={user} />} />
              <Route path="/new-request" element={<NewLeaveRequest user={user} />} />
              <Route path="/manager/new-request" element={<NewLeaveRequest user={user} />} />
              <Route path="/admin/hierarchies" element={<AdminHierarchies />} />
              <Route path="*" element={<Navigate to="/" />} />
            </>
          )}
        </Routes>
      </ConfirmProvider>
    </ToastProvider>
  );
}

export default App;
