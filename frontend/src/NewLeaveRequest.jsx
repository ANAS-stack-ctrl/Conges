import React, { useState, useEffect, useRef } from "react";
import SignatureCanvas from "react-signature-canvas";
import "./NewLeaveRequest.css";
import logo from "./assets/logo.png";
import userIcon from "./assets/User.png";
import { useToast } from "./ui/ToastProvider";
import { useConfirm } from "./ui/ConfirmProvider";

function NewLeaveRequest({ user }) {
  const toast = useToast();
  const confirm = useConfirm().confirm;

  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [includeEnd, setIncludeEnd] = useState(true);
  const [days, setDays] = useState(0);
  const [file, setFile] = useState(null);
  const [comment, setComment] = useState("");
  const [leaveType, setLeaveType] = useState("");
  const [leaveTypes, setLeaveTypes] = useState([]);
  const [isHalfDayValue, setIsHalfDayValue] = useState(false);
  const [halfDayPeriod, setHalfDayPeriod] = useState("FULL");

  const sigCanvas = useRef(null);

  const toISO = (d) => d.toISOString().slice(0, 10);
  const minusOneDay = (iso) => { const d = new Date(iso); d.setDate(d.getDate() - 1); return toISO(d); };

  useEffect(() => {
    fetch("https://localhost:7233/api/LeaveRequest/leave-types")
      .then((res) => res.json())
      .then(setLeaveTypes)
      .catch(() => toast.error("Erreur chargement des types de congé"));
  }, [toast]);

  const selectedType = leaveTypes.find(
    (t) => t.leaveTypeId === parseInt(leaveType || "0", 10)
  );
  const isJustificationRequired = () => selectedType?.requiresProof === true;

  const clearSignature = () => sigCanvas.current?.clear();

  const handleFileChange = (e) => {
    const f = e.target.files?.[0];
    if (!f) return setFile(null);
    const ok = /\.(png|pdf)$/i.test(f.name);
    const mimeOk =
      f.type === "image/png" || f.type === "application/pdf" || f.type === "";
    if (!ok || !mimeOk) {
      toast.warn("Seuls les fichiers PNG ou PDF sont autorisés.");
      e.target.value = "";
      return setFile(null);
    }
    setFile(f);
  };

  useEffect(() => {
    if (!startDate || !endDate) {
      setDays(0);
      return;
    }
    const effectiveEnd = includeEnd ? endDate : minusOneDay(endDate);
    if (new Date(effectiveEnd) < new Date(startDate)) {
      setDays(0);
      return; // le toast est ajouté dans handleSubmit
    }

    fetch(
      `https://localhost:7233/api/LeaveRequest/working-days?startDate=${startDate}&endDate=${effectiveEnd}`
    )
      .then((res) => res.json())
      .then((data) => setDays(data.workingDays))
      .catch(() => setDays(0));
  }, [startDate, endDate, includeEnd]);

  const handleSubmit = async (e) => {
    e.preventDefault();

    // ── Validations de base
    if (!leaveType) return toast.warn("Veuillez choisir un type de congé.");
    if (!startDate || !endDate)
      return toast.warn("Veuillez renseigner les dates de début et de fin.");

    const effectiveEnd = includeEnd ? endDate : minusOneDay(endDate);
    if (new Date(effectiveEnd) < new Date(startDate)) {
      return toast.warn("La date de fin ne peut pas être antérieure à la date de début.");
    }

    if (isJustificationRequired() && !file)
      return toast.warn("Un justificatif est requis pour ce type de congé.");

    if (selectedType?.consecutiveDays > 0 && days > selectedType.consecutiveDays) {
      return toast.warn(
        `Ce type de congé autorise au maximum ${selectedType.consecutiveDays} jour(s).`
      );
    }

    // ── Vérif solde
    try {
      const balanceRes = await fetch(
        `https://localhost:7233/api/LeaveBalanceAdjustment/user/${user.userId}/current-balance`
      );
      const balanceData = await balanceRes.json();
      const needed = isHalfDayValue ? (days > 0 ? 0.5 : 0) : days;
      if (balanceData.balance < needed) {
        return toast.error(
          `Solde insuffisant. Solde: ${balanceData.balance}j, demandé: ${needed}j.`
        );
      }
    } catch {
      return toast.error("Erreur lors de la vérification du solde.");
    }

    // ── Vérif overlap avec une autre demande existante
    try {
      const overlapRes = await fetch(
        `https://localhost:7233/api/LeaveRequest/check-overlap?userId=${user.userId}&startDate=${startDate}&endDate=${effectiveEnd}`
      );
      const overlapData = await overlapRes.json();
      if (overlapData.hasOverlap) {
        return toast.error("Vous avez déjà une demande en cours sur cette période.");
      }
    } catch {
      // Si l'API n'existe pas encore côté backend, ça plantera ici
      console.warn("Vérification overlap indisponible");
    }

    // ── Popup de confirmation
    const typeLabel = selectedType ? selectedType.name : "—";
    const askedDays = isHalfDayValue ? (days > 0 ? 0.5 : 0) : days;

    const ok = await confirm({
      title: "Valider la demande",
      message:
        `Êtes-vous sûr de vouloir valider cette demande ?\n\n` +
        `Type : ${typeLabel}\n` +
        `Du : ${startDate}  Au : ${endDate} ${includeEnd ? "(fin incluse)" : "(fin non incluse)"}\n` +
        `Jours demandés : ${askedDays}\n` +
        (isHalfDayValue ? `Période demi-journée : ${halfDayPeriod}\n` : "") +
        (comment ? `Commentaire : ${comment}\n` : ""),
      okText: "Valider",
      variant: "primary",
      cancelText: "Annuler",
    });
    if (!ok) return;

    // ── Préparation de la signature
    let signatureDataUrl = "";
    if (sigCanvas.current && !sigCanvas.current.isEmpty()) {
      try {
        signatureDataUrl = sigCanvas.current.getCanvas().toDataURL("image/png");
      } catch {
        return toast.error("Erreur lors de la lecture de la signature.");
      }
    }

    // ── Envoi
    const formData = new FormData();
    formData.append("LeaveTypeId", leaveType);
    formData.append("StartDate", startDate);
    formData.append("EndDate", effectiveEnd);
    formData.append("RequestedDays", days);
    formData.append("EmployeeComments", comment);
    formData.append("EmployeeSignatureBase64", signatureDataUrl);
    formData.append("UserId", user.userId);
    formData.append("IsHalfDay", isHalfDayValue);
    formData.append("HalfDayPeriod", halfDayPeriod);
    if (file) formData.append("ProofFile", file);

    try {
      const response = await fetch("https://localhost:7233/api/LeaveRequest", {
        method: "POST",
        body: formData,
      });
      if (!response.ok) {
        const ct = response.headers.get("content-type") || "";
        const err = ct.includes("application/json")
          ? JSON.stringify(await response.json())
          : await response.text();
        throw new Error(err || "Erreur inconnue");
      }

      toast.ok("Demande envoyée avec succès !");
      setLeaveType("");
      setStartDate("");
      setEndDate("");
      setIncludeEnd(true);
      setDays(0);
      setFile(null);
      setComment("");
      setIsHalfDayValue(false);
      setHalfDayPeriod("FULL");
      sigCanvas.current?.clear();
    } catch (error) {
      console.error(error);
      toast.error("Une erreur s'est produite lors de l'envoi de la demande.");
    }
  };

  return (
    <div className="request-container">
      <header className="request-header">
        <img src={logo} alt="Logo" className="logo" />
        <div className="user-info">
          <img src={userIcon} alt="User" className="user-icon" />
          <span>{user?.fullName}</span>
        </div>
      </header>

      <form className="request-form" onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Type de congé</label>
          <select
            value={leaveType}
            onChange={(e) => setLeaveType(e.target.value)}
            required
          >
            <option value="">-- Sélectionnez --</option>
            {leaveTypes.map((type) => (
              <option key={type.leaveTypeId} value={type.leaveTypeId}>
                {type.name}
              </option>
            ))}
          </select>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label>Date de début</label>
            <input
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label>Date de fin</label>
            <input
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              required
            />
            <div style={{ marginTop: 6 }}>
              <label style={{ fontSize: 14 }}>
                <input
                  type="checkbox"
                  checked={includeEnd}
                  onChange={(e) => setIncludeEnd(e.target.checked)}
                  style={{ marginRight: 6 }}
                />
                Inclure la date de fin dans le calcul
              </label>
            </div>
          </div>
        </div>

        <p className="days-info">
          Nombre de jours demandés :{" "}
          {isHalfDayValue ? (days > 0 ? 0.5 : 0) : days} jour(s).
          {!includeEnd && (
            <span style={{ marginLeft: 8, color: "#666" }}>
              (fin non incluse)
            </span>
          )}
        </p>

        <div className="form-group">
          <label>
            Téléverser un justificatif{" "}
            {isJustificationRequired() && (
              <span style={{ color: "red" }}>*</span>
            )}
          </label>
          <input
            type="file"
            name="proofFile"
            accept=".png,.pdf"
            onChange={handleFileChange}
            required={isJustificationRequired()}
          />
        </div>

        <div className="form-group">
          <label>Commentaire :</label>
          <textarea
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="(optionnel)"
          />
        </div>

        <div className="form-group">
          <label>
            <input
              type="checkbox"
              checked={isHalfDayValue}
              onChange={(e) => setIsHalfDayValue(e.target.checked)}
            />{" "}
            Demande en demi-journée
          </label>
        </div>

        {isHalfDayValue && (
          <div className="form-group">
            <label>Période :</label>
            <select
              value={halfDayPeriod}
              onChange={(e) => setHalfDayPeriod(e.target.value)}
            >
              <option value="AM">Matin</option>
              <option value="PM">Après-midi</option>
            </select>
          </div>
        )}

        <div className="form-group">
          <label>Signature :</label>
          <SignatureCanvas
            penColor="black"
            canvasProps={{
              width: 400,
              height: 150,
              className: "signature-canvas",
            }}
            ref={sigCanvas}
          />
          <button type="button" onClick={clearSignature} style={{ marginTop: 5 }}>
            Effacer
          </button>
        </div>

        <button type="submit" className="submit-btn">
          Valider la demande
        </button>
      </form>
    </div>
  );
}

export default NewLeaveRequest;
