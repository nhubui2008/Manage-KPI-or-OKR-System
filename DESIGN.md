---
name: Velzon Operations Console
description: Calm, compact operational UI for daily KPI, OKR, and project work.
colors:
  primary: "var(--primary)"
  primary-dark: "var(--primary-dark)"
  primary-velzon-blue: "#556ee6"
  primary-velzon-blue-dark: "#394da9"
  sidebar-indigo: "#4b63d3"
  sidebar-deep: "#4056bd"
  sidebar-ink: "#f7f8ff"
  sidebar-muted: "#d9deff"
  sidebar-active: "#ffffff"
  canvas: "#f3f3f9"
  surface: "#ffffff"
  border: "#e9ebec"
  ink: "#212529"
  secondary-ink: "#495057"
  muted-ink: "#6d7080"
  control-border: "#ced4da"
  soft-surface: "#f3f6f9"
  row-hover: "#f8f9fb"
  table-heading: "#4b5360"
  auth-sidebar: "#4b63d3"
  success: "#0f6848"
  warning: "#98620e"
  danger: "#d9534f"
typography:
  page-title:
    fontFamily: "hkgrotesk, Poppins, sans-serif"
    fontSize: "1.25rem"
    fontWeight: 600
    letterSpacing: "-0.015em"
  body:
    fontFamily: "Poppins, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "0.8125rem"
    fontWeight: 400
    lineHeight: 1.45
  control-label:
    fontFamily: "Poppins, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 500
    lineHeight: 1.35
  table-label:
    fontFamily: "hkgrotesk, Poppins, sans-serif"
    fontSize: "0.6875rem"
    fontWeight: 600
    letterSpacing: "0.035em"
  navigation-group:
    fontFamily: "hkgrotesk, Poppins, sans-serif"
    fontSize: "0.625rem"
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: "0.08em"
  statistic:
    fontFamily: "Poppins, system-ui, sans-serif"
    fontSize: "1.45rem"
    fontWeight: 600
  public-hero:
    fontFamily: "hkgrotesk, Poppins, sans-serif"
    fontSize: "clamp(2.3rem, 4.4vw, 3.7rem)"
    fontWeight: 600
    lineHeight: 1.08
rounded:
  sm: "3px"
  md: "4px"
  lg: "5px"
  xl: "6px"
  circular: "50%"
spacing:
  "6": "6px"
  "8": "8px"
  "10": "10px"
  "12": "12px"
  "16": "16px"
  "20": "20px"
  "24": "24px"
  "28": "28px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    typography: "{typography.control-label}"
    rounded: "{rounded.md}"
    padding: "6px 12px"
    height: "34px"
  button-primary-hover:
    backgroundColor: "{colors.primary-dark}"
    textColor: "{colors.surface}"
    rounded: "{rounded.md}"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "16px"
  input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    height: "36px"
  navigation-item:
    backgroundColor: "transparent"
    textColor: "{colors.sidebar-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "8px 13px"
    height: "38px"
  table-header:
    backgroundColor: "{colors.soft-surface}"
    textColor: "{colors.table-heading}"
    typography: "{typography.table-label}"
    padding: "9px 12px"
  auth-card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.xl}"
    padding: "28px 30px"
  public-card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "24px"
---

# Design System: Velzon Operations Console

## Overview

**Creative North Star: "Velzon Bright Blue Operations Console"**

Velzon Operations Console is a calm, professional control surface for repeated operational work. Its bright Velzon blue navigation rail, light command bar, pale canvas, and compact white surfaces keep orientation stable while KPI, OKR, project, and administrative data remain the focus.

The system is dense but readable: short controls, restrained corners, thin borders, and limited ambient elevation create a dependable working rhythm. Brand color is functional and tenant-aware; decoration never competes with blocked, overdue, urgent, or assigned work.

**Key Characteristics:**

- Bright Velzon blue navigation paired with a light sticky command bar.
- Compact white operational surfaces on a pale neutral canvas.
- Poppins body copy with HK Grotesk-led headings and labels.
- Tenant-aware primary actions and visible keyboard focus.
- Desktop information density that resolves to a single-column mobile flow.

## Colors

The palette is a cool operational neutral system anchored by indigo navigation and a tenant-configured primary action color.

### Primary

- **Tenant Action Color:** Drives primary buttons, focus accents, active markers, and AI entry points; its darker runtime companion owns hover states.
- **Operations Indigo:** Defines the persistent navigation rail and keeps global orientation visually separate from work content.

### Neutral

- **Pale Canvas:** Separates the application frame from white cards without decorative gradients.
- **White Surface:** Carries cards, fields, menus, modals, the command bar, and footer.
- **Quiet Border:** Delineates operational regions with thin, low-contrast strokes.
- **Working Ink:** Carries primary content; secondary and muted ink support metadata and placeholders.
- **Soft Utility Surface:** Marks table headings, hover states, and quiet interactive feedback.

**The Functional Color Rule.** Use color to communicate navigation, state, focus, or action; do not turn operational pages into decorative color fields.

## Typography

**Display Font:** HK Grotesk (with Poppins and sans-serif fallbacks)

**Body Font:** Poppins (with system UI and Segoe UI fallbacks)

**Character:** HK Grotesk gives headings and compact labels crisp operational authority. Poppins keeps repeated controls and Vietnamese body copy neutral, legible, and familiar.

### Hierarchy

- **Page Title:** Semibold and compact, with slightly tightened tracking; use once for page orientation.
- **Statistic:** Semibold tabular-scale emphasis for KPI and OKR values, without oversized dashboard theatrics.
- **Body:** Regular-weight copy at the shell baseline with a relaxed line height for dense content.
- **Control Label:** Medium-weight compact type for buttons and direct actions.
- **Table Label:** Semibold uppercase text with modest tracking for scan-friendly columns.
- **Navigation Group:** Small semibold uppercase labels with wider tracking to separate navigation domains.

**The Compact Hierarchy Rule.** Create hierarchy with weight, role, and restrained scale changes; never use oversized display typography inside the authenticated console.

## Layout

The authenticated shell uses a fixed vertical sidebar, a sticky command bar, and a fluid content canvas. On desktop, the rail is 70px collapsed or 250px expanded, the command bar is 70px high, and page content uses 20px 24px 28px outer padding. Content grids stay compact and prioritize current operational state before secondary analysis.

At 991.98px and below, the 250px sidebar becomes an off-canvas panel, the main content returns to zero left offset, and a dark overlay protects focus. At 767.98px, page padding becomes 16px and the full search field is hidden. At 575.98px, the command bar becomes 62px high, page padding becomes 12px, actions tighten, and dashboards resolve into the single-column reading order shown by the mobile reference.

**The First-Viewport Rule.** Put blocked, overdue, urgent, assigned, and summary state before secondary charts or explanatory content.

### Authentication and public surfaces

Authentication uses the same indigo, canvas, border, typography, and focus vocabulary as the authenticated console. Desktop authentication is a split composition with an indigo product panel and a compact white form card; mobile removes the decorative panel and keeps the form as the first reading surface.

The public site uses the same restrained rectangular components at a more generous marketing scale. Hero, feature, workflow, pricing, registration, and footer sections may use additional whitespace, but they do not introduce gradients, glass, oversized radii, or a competing visual system.

**The One-Product Rule.** Public, authentication, operational, and SaaS administration routes must look like modes of one Velzon product rather than separate templates.

## Elevation & Depth

Depth is restrained and structural. Cards and the command bar rest on a nearly flat ambient shadow; card hover increases separation slightly. Floating menus, search results, settings panels, modals, and the AI panel use progressively stronger shadows because they temporarily sit above the working plane.

### Shadow Vocabulary

- **Resting Surface:** A nearly flat shadow for cards and the command bar.
- **Hover Surface:** A small lift for actionable cards without translation.
- **Floating Menu:** A compact shadow for dropdowns and transient result panels.
- **Modal Surface:** A broader shadow for modal dialogs and right-side settings panels.
- **Off-canvas Navigation:** A directional shadow that separates the mobile sidebar from its overlay.

**The Flat-by-Default Rule.** Keep ordinary content surfaces quiet; reserve obvious elevation for hover feedback and temporary layers.

## Shapes

The console uses restrained rectangular geometry. Most controls, cards, navigation items, and dropdowns use the medium corner; badges use the smaller corner, while modals and floating AI surfaces use the larger steps. Circles are reserved for icon-only controls, avatars, and the AI launcher. Thin borders define surfaces more often than shadow.

**The Four-Pixel Rule.** Default to the medium corner for operational UI and depart from it only for compact badges, elevated containers, or explicitly circular controls.

## Components

### Buttons

- **Shape:** Compact rectangular controls with the medium corner and a 34px minimum height.
- **Primary:** Tenant action color with white text and compact horizontal padding.
- **Hover / Focus:** Hover switches to the tenant dark color with a small ambient shadow; keyboard focus uses a visible two-pixel primary-tinted outline.
- **Icon-only:** Header controls are 38px circles with a quiet border and utility-surface hover state.

### Cards / Containers

- **Corner Style:** Medium corner for normal cards; larger corners only for modals and floating panels.
- **Background:** White surfaces on the pale canvas.
- **Shadow Strategy:** Nearly flat at rest, gently stronger on hover, with no transform.
- **Border:** One-pixel quiet border.
- **Internal Padding:** 16px for bodies and statistic cards; compact headers use 12px 16px.

### Inputs / Fields

- **Style:** White fields, one-pixel control border, medium corner, and 36px minimum height.
- **Focus:** Primary-tinted border plus a three-pixel translucent focus ring.
- **Search:** The command-bar search uses a pale canvas fill and is hidden at the compact-content breakpoint.

### Navigation

- **Style:** Light navigation text on indigo, compact 38px rows, muted uppercase group labels, and a darker footer zone.
- **State:** Hover uses a translucent white wash; active and pending items use a stronger wash, white text, medium weight, and a narrow tenant-colored left marker.
- **Responsive:** Desktop supports collapsed and expanded rail states; mobile always reveals the full 250px rail as an off-canvas panel with overlay.

### Tables and Status

- **Tables:** Compact rows, tabular numerals, uppercase utility-surface headings, quiet dividers, and a soft row hover.
- **Status:** Small, medium-weight badges with restrained three-pixel corners; semantic color belongs to the state, not decoration.

## Do's and Don'ts

### Do:

- **Do** keep controls, cards, and tables compact enough for repeated daily operations.
- **Do** preserve the dark navigation, light command bar, pale canvas, and white-surface hierarchy.
- **Do** surface blocked, overdue, urgent, and assigned work early in the reading order.
- **Do** keep keyboard focus visible and provide reduced-motion behavior.
- **Do** let tenant configuration own the primary action color.

### Don't:

- **Don't** use marketing-style hero layouts or oversized cards inside authenticated workflows.
- **Don't** add decorative gradients, glass effects, blobs, or ornamental dashboard chrome.
- **Don't** hide workflow actions behind ambiguous affordances or excessive scrolling.
- **Don't** use oversized typography or vague AI-generated copy as a substitute for operational hierarchy.
- **Don't** change KPI, OKR, authorization, validation, or data contracts to satisfy a visual treatment.

## Implementation contract

- Load the local Velzon `app.min.css` on every route, followed by the application compatibility layer.
- Preserve existing Razor actions, antiforgery fields, authorization checks, element IDs, `data-*` hooks, Bootstrap behavior, and instant-navigation script staging.
- Keep Velzon template demo scripts (`app.js`, `layout.js`, `plugins.js`) out of the app because the existing application shell owns navigation and interaction state.
- Treat legacy module CSS as behavioral compatibility only; `velzon-kpi.css` is the final visual authority.

## Finish review — 2026-08-11

Verdict: **PASS**. Desktop and mobile Chrome checks cover authentication, dashboard, KPI, OKR, projects, check-ins, people, evaluation, catalog, SaaS administration, public landing, and representative create forms. Active rendered surfaces use the Velzon palette with no glass treatment or decorative gradients, preserve keyboard focus, and do not create document-level horizontal overflow.
