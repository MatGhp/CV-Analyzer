# Frontend MVP — Resume Upload, Analyze, Results

This document defines the minimum front-end we’ll ship to upload a resume, run AI analysis, and display results.

## User story

As a job seeker, I want to upload my resume and get an AI-powered analysis with a score and actionable suggestions, so I can quickly improve my CV before applying.

Acceptance criteria:
- I can upload a resume file (PDF or DOCX) up to 5 MB and see clear validation errors if not valid.
- I can start the analysis and see a visible progress state (uploading → analyzing).
- I receive a result that includes:
  - An overall score (0–100).
  - A brief summary of strengths/risks.
  - A list of categorized suggestions with priority.
  - Optionally, an “optimized” resume text block I can copy.
- I can retry if something fails and see a helpful error message.
- The UI works on desktop and mobile and is accessible via keyboard and screen readers.

## MVP features

- Resume upload
  - Drag & drop + “Choose file” button.
  - Inline validation: type (PDF/DOCX), size (≤5 MB).
  - Clear file chip with filename and remove action.

- Analyze trigger
  - Primary “Analyze resume” button (disabled until a valid file is selected).
  - Upload to backend, then trigger analysis for the created resume.

- Progress + status
  - Determinate progress while uploading (optional) and indeterminate spinner while analyzing.
  - Non-blocking toast/snackbar for background events and errors.

- Results view
  - Score badge (0–100) with color scale (e.g., <50 red, 50–74 amber, 75+ green).
  - Highlights: 2–3 key takeaways (strengths/risks).
  - Suggestions list:
    - Group by category (Formatting, Keywords, Experience, Clarity).
    - Priority indicator (1–5) as visual tag.
    - Short, actionable descriptions.
  - Optimized text (optional): Expandable panel with copy-to-clipboard.
  - Metadata footer: analysis time, model label.

- Error/empty states
  - Friendly empty state before upload.
  - Retry CTA for network/analysis errors.

- Basic settings/telemetry (MVP)
  - Minimal logging for errors (no PII in logs).
  - No auth for v1 (assume single-user local dev).

## Design (minimal UI/UX)

- Information architecture
  - Single-page layout:
    - Section 1: Upload + Analyze card.
    - Section 2: Results panel (appears after success).
  - Routes (for future):
    - / (upload)
    - /results/:id (direct link to analysis; optional for v1)

- Layout and components
  - Header: “CV Analyzer” + subtle tagline.
  - Upload card:
    - Dropzone area with cloud icon, “Drag & drop or choose file” text.
    - Supported types hint: “PDF, DOCX · up to 5 MB”.
    - File chip shows name + remove (X).
    - Primary button: “Analyze resume”.
  - While analyzing:
    - Overlay loader on upload card, disable interactions.
    - Status line: “Analyzing your resume… this may take up to 20s”.
  - Results panel:
    - Score badge large and prominent on the left; short summary on the right.
    - Suggestions: vertical list with category chip + priority tag + short description.
    - Optimized content: collapsible area with monospace-style block and “Copy” button.
    - Secondary actions row: “Re-analyze” and “Upload another resume”.

- Visual style
  - Neutral, clean theme: white surface, subtle shadows, ample spacing.
  - Color accents for score and priority chips.
  - Typography: one strong heading (H3), clear body text.
  - Buttons: High-contrast primary, outline secondary.

- Accessibility
  - Keyboard navigable: Tab order from upload → analyze → results actions.
  - ARIA roles:
    - role="button" on dropzone with aria-label.
    - aria-live="polite" for progress and result arrival.
  - Color contrast compliant (WCAG AA).
  - Error messages linked to inputs via aria-describedby.

- Responsive behavior
  - Mobile: Stack layout; score badge above summary; suggestions become full-width cards.
  - Desktop: Two-column layout for score/summary next to suggestions.

- Empty, loading, error states (microcopy)
  - Empty: “Upload your resume to get instant AI feedback.”
  - Loading: “Analyzing your resume…”
  - Error: “We couldn’t complete the analysis. Please try again.”

## Data contract (aligned with backend/agent)

```ts
type ResumeAnalysisResponse = {
  score: number; // 0–100
  summary?: string;
  suggestions: Array<{
    category: 'Formatting' | 'Keywords' | 'Experience' | 'Clarity' | string;
    description: string;
    priority: 1 | 2 | 3 | 4 | 5;
  }>;
  optimized_content?: string;
  metadata?: { analyzed_at?: string; model?: string };
};
```

## Tiny scope checklist for implementation

- Upload + validation card with drag-and-drop.
- POST to backend, then trigger analyze; show spinner until done.
- Render score, suggestions, and optional optimized block.
- Copy-to-clipboard and retry actions.
- Accessibility basics and responsive layout.
