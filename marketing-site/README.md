# Sisonke Marketing Site

Static marketing landing page for **Sisonke** — a digital management platform for stokvels, burial societies, funeral parlours and community finance groups. A product by **PEO Capital Holdings**.

---

## Purpose

This folder contains a standalone, fully static marketing site for Sisonke by PEO Capital Holdings. It is completely separate from the main `Sisonke.Web` ASP.NET Core / Blazor application.

The marketing site serves as the public-facing landing page, explaining the product positioning, solution areas and value proposition to potential users before they join the pilot or sign up.

---

## Files

| File | Description |
|---|---|
| `index.html` | Main landing page — all sections, semantic HTML |
| `styles.css` | All styling — CSS custom properties, responsive, no dependencies |
| `README.md` | This document |

---

## How to Open Locally

No build tools, no npm, no server required.

**Windows:**
1. Navigate to this folder in File Explorer
2. Double-click `index.html`

Or from PowerShell:
```powershell
Start-Process index.html
```

Or from the terminal:
```bash
start index.html       # Windows
open index.html        # macOS
xdg-open index.html    # Linux
```

Any modern browser (Chrome, Edge, Firefox, Safari) will render the full site.

---

## Sections

1. **Hero** — Sisonke headline, subtitle and mock dashboard visual
2. **Problem statement** — why paper, WhatsApp and spreadsheets aren't enough
3. **Features** — 8 feature cards covering every part of the workflow
4. **Role-based workspaces** — Member, Secretary, Chairperson, Treasurer, Admin
5. **Claim workflow** — 5-step burial society claim process
6. **Why Sisonke** — 6 differentiators specific to South African societies
7. **Pricing** — Starter, Growing Society, Enterprise (placeholder amounts)
8. **Call to action** — pilot programme invite and contact email
9. **Footer** — brand and copyright

---

## Relationship to Sisonke.Web

This marketing site is **completely separate** from the main application.

| | Marketing Site | Sisonke.Web |
|---|---|---|
| Type | Static HTML / CSS | ASP.NET Core Blazor |
| Purpose | Public marketing | Stokvel management app |
| Database | None | SQLite (dev) / Azure SQL (hosted) |
| Framework | None | .NET 10 |
| Build required | No | Yes (`dotnet build`) |
| Hosting | Any static host | Azure App Service |

Changes to `Sisonke.Web` do not affect this site, and vice versa.

---

## Future Deployment Options

When ready to host publicly, this static site can be deployed to:

- **Azure Static Web Apps** (free tier) — recommended; integrates with GitHub Actions
- **GitHub Pages** — free for public repositories
- **Azure Blob Storage** with static website hosting enabled
- **Netlify / Vercel** — free tier available, instant deploys from Git

A custom domain (`sisonkestokvel.co.za`, `app.sisonkestokvel.co.za` or similar) can be connected after deployment.

---

## Design

- Cream background (`#FAF7F2`)
- Deep forest green (`#0F2419` / `#1b5a40`)
- Gold accents (`#C9922A` / `#E8B84B`)
- System fonts only — no Google Fonts, no CDN
- Fully responsive — mobile, tablet, desktop
- Accessible: skip link, semantic headings, ARIA labels, meaningful button text
- Zero JavaScript

---

## Status

Early marketing placeholder, aligned with the Sisonke Azure test deployment preparation (see `docs/AZURE_TEST_DEPLOYMENT_RUNBOOK.md`).

Content, copy and pricing will evolve before public launch.
