// src/components/ProfilePopup.jsx
import React from "react";
import "./profilePopup.css";

export default function ProfilePopup({ open, onClose }) {
  if (!open) return null;

  return (
    <div className="pCard-backdrop" onClick={onClose}>
      <div
        className="pCard-container"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <button className="pCard-close" onClick={onClose}>
          ×
        </button>

        <div className="pCard-grid">

          {/* Imagen */}
          <div className="pCard-imageBox">
            <img
              src="C:\IMGSQL\PruebaCv.png"
              alt="Foto"
              className="pCard-image"
            />
          </div>

          {/* Contenido */}
          <div className="pCard-content">
            <header>
              <h2 className="pCard-title">ALBERTO HARTH</h2>
              <p className="pCard-role">SOCIO FUNDADOR · PRESIDENTE</p>
            </header>

            <section className="pCard-section">
              <h3 className="pCard-sectionTitle">EDUCACIÓN</h3>
              <ul className="pCard-list">
                <li>MIT PhD Estudios Urbanos y Planificación</li>
                <li>MIT Master en Planificación Urbana</li>
                <li>UES Arquitecto</li>
              </ul>
            </section>

            <section className="pCard-section">
              <h3 className="pCard-sectionTitle">MIEMBRO</h3>
              <p>AAU, APA, ASIA, CODES</p>
            </section>

            {/* BIO con scroll */}
            <section className="pCard-section pCard-bio">
              <p>
                Alberto, Doctor en Estudios Urbanos del MIT, cuenta con 20 años
                de experiencia en desarrollo urbano...
              </p>
              <p>
                2004 Azuma Terrazas San Cristóbal, 2008 Reforma Maestro...
              </p>
            </section>

          </div>
        </div>
      </div>
    </div>
  );
}
