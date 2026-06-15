# AnatomiQ — UX Architecture & Visual Identity

> **Purpose:** Defines navigation, information architecture, screen inventory, interaction patterns, and visual identity constraints for AnatomiQ. Read before any UI-related chat.
>
> **Scope and limits:** This document defines the *structure* of the experience and the *constraints* on its appearance. It deliberately does not specify pixel-precise visual design. Final visual design should be iterated against real screens, ideally with input from a designer or real users. Decisions made at the planning stage that go beyond constraints would be opinions dressed up as a plan.
>
> **What this document is NOT:** wireframes, mockups, brand guidelines, design system, color codes that survive contact with users.

---

## Part A — Core UX Principles

Five principles that resolve most UX questions before they arise.

### A.1 The body is the hero

The 3D anatomical body is the reason someone opens AnatomiQ. UI exists to support exploration of the body, not the other way around. Concretely:

- Default screen state is "body fills the viewport" — UI is overlaid, minimized, or contextual
- Persistent UI (top/bottom bars) takes the smallest space that's still functional
- Modal screens that hide the body completely are reserved for tasks where the body isn't relevant (settings, history, account)
- When a UI panel must appear, it covers a defined portion of the screen (typically 30–40%) — the body remains visible alongside

This is the single most important UX rule in AnatomiQ. Every other UX decision is in service of it.

### A.2 AR is a mode, not a wrapper

AR mode is one of three viewing modes (AR Surface, AR Space, 3D Viewer per ATLAS-003). Switching between them must be frictionless and the rest of the app must work identically in all three. Concretely:

- Same controls, same gestures, same interactions across all three modes
- Mode switch is a single tap from a persistent control
- No "AR-only" features other than the AR camera itself
- Failure of AR (tracking lost, ARCore unavailable) silently falls to 3D Viewer mode without a "AR is broken" wall

### A.3 Progressive disclosure

Most users see <30% of the app's functionality. Surface what's most useful immediately; reveal complexity on demand. Concretely:

- Default view shows the body and primary action affordances only
- Layer toggles, Q&A, disease selection, etc. are accessible via clear paths — but not all visible at once
- Settings, history, account-level features live in a single "more" entry point
- Onboarding shows three core actions, not a feature tour

### A.4 One-handed reachability

Phone is held with one hand. Primary actions live in the bottom 60% of the screen. Top bar is for status and "back" only. Specifically:

- Primary action bar at bottom of screen (thumb zone)
- Touch targets minimum 44pt on the touch axis
- Two-handed interactions (pinch zoom on body) are fine — primary navigation is one-handed
- Avoid top-right corner placement for any frequent action (hardest to reach with right thumb)

### A.5 Failure recovery is part of the UX

The app degrades, doesn't break (per CORE-007). Every failure mode has a UI treatment, not just a log line. Concretely:

- AR tracking lost → on-screen indicator + suggestion to recover, not a modal blocker
- API offline → "thinking..." indicator with cached response when ready, never a wall
- Performance throttle → quiet visual quality reduction, optional explanation icon
- Permission denied → in-context request, not a generic OS denial screen

---

## Part B — Navigation Model

### B.1 Top-level structure

**Three-pillar tab bar at bottom**, plus persistent profile / more access:

```
┌─────────────────────────────────────────┐
│         [body fills the screen]         │
│                                         │
│                                         │
├─────────────────────────────────────────┤
│  [ATLAS]    [PRISM]    [CADENCE]   [···]│ ← bottom navigation
└─────────────────────────────────────────┘
```

Three primary tabs (one per pillar), one "more" tab for settings, history, profile, attribution. **Three primary tabs match research (3-5 is the sweet spot for bottom nav).** Adding RehabAR later → four pillars; still within thumb reach.

**Why bottom tabs over hamburger menu:**
- AnatomiQ's three pillars are equally important and parallel — users will switch between them, not navigate down a hierarchy
- Hamburger menus hide structure, making feature discovery worse
- Native Android convention favors bottom navigation for app-level structure
- Maintains thumb reachability principle

### B.2 Within-tab structure

Each pillar tab is a stack — a primary screen with modal/overlay drill-downs:

```
ATLAS tab
 ├─ ATLAS Home (primary screen — body + layer/select controls)
 │   ├─ Disease Cascade overlay (modal)
 │   ├─ AI Q&A panel (slide-up sheet)
 │   ├─ Layer Toggle panel (slide-up sheet)
 │   ├─ Interconnectivity Explorer overlay (modal)
 │   └─ Time Slider control (inline overlay, only when cascade active)
 └─ Disease Picker (full-screen)

PRISM tab
 ├─ PRISM Home (mode selection: Patient or Doctor)
 │   ├─ Patient Mode → Symptom dialogue flow (sequential screens)
 │   └─ Doctor Mode → Scan importer + body model
 └─ Saved sessions list

CADENCE tab
 ├─ CADENCE Home (scenario library)
 │   └─ Procedure walkthrough (full-screen AR)
 └─ Performance history

MORE tab
 ├─ Settings (audience level, language, performance, cache)
 ├─ Profile / role (general / patient / student / clinician)
 ├─ History
 ├─ Attribution & licenses (Z-Anatomy/BodyParts3D required attribution here)
 └─ About
```

### B.3 Modal vs. overlay vs. sheet — when to use each

| Pattern | Use when | Example |
|---|---|---|
| **Bottom sheet** (slide-up panel, body still partly visible) | The action is contextual to what's currently on screen and the body should remain visible | Q&A panel, Layer toggle panel, Organ info card |
| **Full-screen modal** (covers everything, dismiss to return) | Linear task that requires focus and where the body isn't directly involved | Disease picker, Symptom dialogue, Scan importer, Settings |
| **Inline overlay** (floats over body without occluding much) | Persistent control during a specific activity | Time slider during cascade, Cascade narration text |
| **Full-screen takeover** (replaces tab — temporary) | Active AR procedure that requires all real estate | CADENCE procedure walkthrough |

**Default to bottom sheets.** They preserve the body-as-hero principle. Use full-screen only when the body genuinely isn't relevant.

### B.4 Back navigation

- System back button always works (Android convention)
- Bottom sheets dismiss with swipe-down or tap-outside
- Modals have an explicit close button (X in top-left, not top-right — reachability)
- Tab switches do NOT add to back stack — switching from ATLAS to PRISM and back is two taps, not back-back

---

## Part C — Screen Inventory

The complete set of screens the app needs. Each is a placeholder for design work, not a specification of the design.

### C.1 First-run and orientation

**Screen: Welcome**
Single screen. Three lines of text and one button. *"AnatomiQ. See the Body. Understand Everything. Get started."* No carousel, no feature tour.

**Screen: Permission requests** (camera, in-context)
Triggered when the user first enters AR mode, not at app launch. Explains *why* the camera is needed in one sentence before the OS prompt.

**Screen: Audience level selection** (one-time at first launch, changeable in Settings)
"How would you describe yourself?" with four cards: General, Patient, Medical Student, Clinician. Drives the AI prompt audience parameter throughout.

### C.2 ATLAS screens

**Screen: ATLAS Home**
The default screen of the app. Body model rendered in the chosen viewing mode. Persistent controls:
- Top bar (minimal): mode indicator (AR Surface / AR Space / Viewer), tracking state badge if relevant
- Bottom action bar: [Layer], [Disease], [Ask], [Mode]
- Empty state when nothing selected: subtle hint to tap an organ

**Sheet: Layer Toggle**
Slide-up bottom sheet. Six layer buttons in a 2×3 grid. Visual indication of active layer(s). Notification when 2-layer limit is reached.

**Sheet: Organ Info Card** (appears when an organ is tapped)
Small card at bottom showing: organ name, brief description, and three actions: [Ask about this], [Show connections], [Hide].

**Modal: Disease Picker**
Full-screen. Searchable list of available diseases. Each card shows: disease name, category icon, severity indicator, brief description. T2D / Hypertension / CKD for academic build.

**Overlay: Time Slider** (visible only during cascade)
Floating horizontal slider at the bottom-third of the screen. Shows current stage label. Drag to scrub through cascade. Play/pause button at left.

**Sheet: AI Q&A**
Slide-up panel covering bottom 50%. Conversation thread. Text input at bottom. Voice input icon (P3, deferred). Suggestions of common questions when empty. AI responses can include "[Show on body]" actions that highlight referenced organs.

**Modal: Interconnectivity Explorer**
Full-screen. Body remains visible but enters "explorer" rendering — connections drawn as visible lines. Tapping a connection shows a card explaining the relationship.

### C.3 PRISM screens

**Screen: PRISM Home**
Mode selection. Two large cards: "Explore symptoms (Patient)" and "Clinical tools (Doctor)". The patient card is significantly more prominent — it's the more common use case.

**Flow: Symptom Dialogue (Patient Mode)**
Sequence of screens, one question per screen — this is critical, not a single long form.
1. Body map intake — full body shown, "Tap where you feel it"
2. Dialogue screens — each screen shows AI question and input area
3. Visualization checkpoint — when AI references anatomy, body model appears with relevant areas highlighted
4. Summary screen — what was discussed, key categories of relevance, next steps
5. Report option — "Save and share with your doctor?" → generates PRISM-006 report

**Screen: Symptom Report**
Generated report from PRISM-006. Read-only. Share button (Android share sheet) and Save button.

**Screen: Scan Importer (Doctor Mode)**
File picker UI. Recent scans list. After selection: scan preview with "Analyze" button.

**Screen: Scan Annotation View (Doctor Mode)**
Scan image fills the screen. Annotations appear as tappable rectangles. Tapping shows the structured observation. Toggle to show body model alongside (split-screen) for explanation.

### C.4 CADENCE screens

**Screen: CADENCE Home**
Scenario library. Cards showing each procedure: name, difficulty, estimated duration, anatomical region.

**Screen: Procedure Walkthrough** (full-screen takeover)
Camera view fills the screen. AR overlays guide step-by-step. UI elements:
- Step counter top-left ("Step 3 of 8")
- Step description at the top, expanded on tap
- "Pause", "Repeat step" actions at bottom
- Real-time movement feedback as colored overlays (green/yellow/red per CADENCE-002)

**Screen: Performance Summary**
Post-procedure report. Score, per-step accuracy, AI-generated improvement suggestions. Compare to previous sessions if available.

### C.5 MORE tab screens

**Screen: Settings**
- Audience level (General / Patient / Student / Clinician)
- Language (deferred — English only at launch)
- Performance (Performance / Balanced / Quality)
- Reset cached AI responses
- Privacy / data handling

**Screen: Attribution**
Static screen with required Z-Anatomy / BodyParts3D / Open3DModel attribution. CC BY-SA license text. Library credits.

**Screen: History**
Recent disease cascades viewed, recent symptom sessions, recent procedures attempted.

### C.6 Universal screens

**Component: Loading states** (not a screen, but a pattern)
Three loading patterns:
- Quick (<1s) — silent, no indicator
- Medium (1–4s) — subtle pulse on the relevant element ("thinking...")
- Long (>4s) — explicit progress with explanation ("Analyzing scan, this can take 30 seconds...")

**Component: Error states**
- Network failure: inline non-blocking banner ("Working offline — using cached responses")
- AR tracking lost: floating toast that fades when tracking returns
- AI unavailable: "AI is unavailable right now. Showing pre-loaded information instead." (cached fallback shown beneath)
- Crash recovery: gentle reopen-to-last-state, no "AnatomiQ stopped" dialog

**Component: Empty states**
Every list, every screen, has a defined empty state. None should ever be "blank." Examples:
- No history yet → "Your history appears here as you explore. Try opening a disease cascade."
- No saved sessions → "Sessions you save show up here. Generate a report from a symptom exploration to save it."

---

## Part D — Interaction Patterns

### D.1 Body model interactions (consistent across all modes)

| Gesture | Action |
|---|---|
| Single tap on organ | Select organ → highlight + organ info card |
| Single tap on empty space | Deselect / dismiss info card |
| Double tap on organ | Zoom to organ (frame body to focus on it) |
| Pinch | Zoom (3D Viewer mode); scale model (AR modes) |
| Two-finger drag | Rotate model (3D Viewer mode); reposition (AR modes) |
| Long-press on organ | Context menu (Ask, Show connections, Hide) |
| Swipe left/right on cascade UI | Step forward/back through cascade manually |

These gestures **do not change** based on mode. AR Surface, AR Space, and 3D Viewer all respond identically. This is how A.2 (AR is a mode, not a wrapper) is enforced concretely.

### D.2 Cascade playback interactions

- **Disease selected** → cascade pre-loads and auto-plays from step 0
- **Tap anywhere** during cascade → pause, show controls
- **Time slider drag** → scrub forward/backward through stages
- **Tap "Skip to end"** → jumps to advanced stage
- **Cascade complete** → returns to ATLAS Home with "Replay" / "Explore" / "Pick another disease" actions

Cascade does NOT replay automatically. User has to choose to see it again.

### D.3 Q&A interactions

- **Slide-up Q&A sheet** → input field focused, keyboard appears
- **Type question** → submit
- **AI response streams in** (not all at once) — feels responsive even if total response takes 5+ seconds
- **AI references an organ** → "[Show this on the body]" inline action that highlights the organ when tapped
- **Common questions suggestions** when sheet opens fresh (not when there's history)

### D.4 Permission request pattern

Never request permissions at app launch. Request in the moment they're needed:

```
User opens ATLAS Home → app opens in 3D Viewer mode
User taps "AR Mode" → app explains in one sentence why camera is needed → OS permission prompt
```

Never:
- Multiple permission walls before showing any content
- Generic "we need permissions" screens
- Re-prompting after denial without context

If permission denied: app continues to function in 3D Viewer mode silently. A subtle "Tap to enable AR" persists in the mode switcher.

### D.5 First-time-in-feature hints

When a user enters a feature for the first time, a small hint appears for 3 seconds, then auto-dismisses:

- First time on ATLAS Home → "Tap any part of the body to learn about it"
- First time opening a cascade → "Drag the slider to control time"
- First time in PRISM patient mode → "Tap where you feel it on the body, then describe what you're feeling"

Hints are contextual, brief, and never blocking. Do not require dismissal.

---

## Part E — Accessibility

### E.1 Required from launch

- **Touch targets** ≥ 44pt × 44pt for all interactive elements
- **Text contrast** ≥ 4.5:1 for body text, ≥ 3:1 for large text (WCAG AA)
- **Text resizing** support — UI scales when system font size increases
- **Color is never the only signal** — every color-coded state also has shape, icon, or label
- **Screen reader labels** for every button, every interactive element, every body part highlight
- **No flashing animation** faster than 3 Hz (seizure safety)

### E.2 Specific to AnatomiQ

- **Cascade animation** has a "Hide animations" toggle that uses snapshots between stages instead of continuous animation (vestibular safety + battery)
- **Voice narration** for cascade steps is the audio version of `narrationFallback` text — already authored, just needs TTS
- **AR mode is never required** — every feature works in 3D Viewer
- **Audience level setting** affects vocabulary, which is itself an accessibility feature for non-medical users

### E.3 Deferred but planned

- **Voice input** for Q&A (depends on Whisper integration, P3)
- **High contrast mode** with simplified materials on the body model
- **Localization** — UI strings externalized from code from day 1, even if only English is shipped

---

## Part F — Visual Identity Constraints

This section sets directional constraints. It does not specify a final design system. A designer or design-iteration process produces the final look.

### F.1 Aesthetic direction

**Clinical, not stylized.** AnatomiQ presents itself as serious medical content. Sources of design inspiration:

- ✅ Modern clinical software (Epic Hyperdrive, Doximity, UpToDate)
- ✅ Premium medical references (Complete Anatomy, Visible Body)
- ✅ Calm, technical interfaces (Linear, Notion, Apple Health)
- ❌ Gamified/colorful health apps
- ❌ Cartoon or playful illustration style
- ❌ Wellness-app gradients and emoji-heavy interfaces

This is a tool for understanding the body. The visual language should reinforce that it can be trusted with medical content.

### F.2 Color system constraints

Rather than specifying a palette now, these are the rules a designer will work within:

- **Single neutral foundation** (whites, grays, near-blacks) — UI surfaces use neutral hues so the body is the visual focus
- **Single accent color** for primary actions, focused states, and brand identity. Suggest a calm clinical blue or teal (similar in feel to the existing project documentation). Don't pick the exact hex now.
- **Functional colors only** for state communication: green = success/correct, yellow = caution/attention, red = error/severe. Used sparingly, never decoratively.
- **Cascade severity gradient** (visible on body): yellow → orange → red as severity increases. Defined in the cascade JSON `visualEffect.color` field; current hex values in the existing cascades are placeholders subject to designer adjustment.
- **Dark mode required.** AR camera content tends to be dark; light UI fights it. Dark mode is the primary mode; light mode is supported.

### F.3 Typography constraints

- **One sans-serif type family** for all UI. Not curly, not display, not "futuristic." Inter, Source Sans, or system default (San Francisco / Roboto) all fit.
- **Two weights maximum** in regular use: regular and medium-bold. A third weight (light) reserved for very large display text only.
- **Three or four sizes** total: body, small, label, large display. No 12-different-size typography.
- **Line height** generous on body text (≥1.5x) — medical content has long sentences that need breathing room.
- **No serifs in UI.** A serif could be used for cascade narration text *if* a designer demonstrates it improves reading; default is sans throughout.

### F.4 Iconography

- **Outline icons, not filled.** Less visually heavy when the body is the focus. Heroicons, Phosphor (Outline), or Material Symbols (outlined variant) all work.
- **Single weight** for all icons. Don't mix outlined with filled.
- **Avoid unique custom icons for common actions.** Use familiar conventions (back, forward, settings, share). Custom illustration is reserved for specific medical concepts.

### F.5 Body model rendering style

This is a real decision that affects everything: what does the 3D body look like?

**Recommendation: realistic-clinical, not photorealistic, not stylized.**

- ✅ Anatomically accurate proportions and detail
- ✅ Subtle materials that read clearly as flesh, bone, vessel without looking "artistic"
- ✅ Colors close to anatomical reality (red blood vessels, beige bone, pink organs) but slightly desaturated for visual calm
- ❌ Photorealistic with skin texture and hair (uncanny + heavier rendering)
- ❌ Cartoonish bright colors and flat shading (undermines medical credibility)
- ❌ Wireframe-only or fully transparent "sci-fi" look (unhelpful for the educational purpose)

Reference points: Complete Anatomy's body model is roughly the right register. Polished, realistic, but designed for educational reading rather than visual realism.

When materials are baked from Z-Anatomy, this means: keep the per-system color coding the source provides (skeletal = ivory, muscle = red, vascular = red/blue, nervous = yellow), but reduce overall saturation by 15-25% and lift the value range so dark organs stay visible against dark mode UI.

### F.6 Motion and animation

- **Purposeful, not decorative.** Every animation has a function: showing change, providing feedback, drawing attention.
- **Fast.** UI animations 150–250ms. Cascade animations longer (per the cascade JSON).
- **Easing curves** are gentle. Cubic-bezier eases that read as natural, not bouncy. No spring overshoot in clinical UI.
- **Body model cascade animations** are the exception — they're paced for human comprehension (~3s per step) and are the visual centerpiece.
- **Reduce motion** setting respected throughout.

### F.7 Logo and app icon

Defer. A wordmark for the academic build can be the word "AnatomiQ" set in the chosen typeface with a small accent — no formal logo design needed. App icon can be a simple letter mark on the accent color.

If the project moves toward startup phase, commission proper identity work then. Do not invent a logo at the planning stage.

### F.8 What's intentionally NOT specified

- Exact hex codes
- Specific typeface choice
- Pixel-precise spacing scale
- Final iconography set
- Logo design
- Splash screen / onboarding visuals
- Marketing visual identity

These all benefit from iteration against real screens. Pre-specifying them at the planning stage produces decisions that don't survive contact with the actual app.

---

## Part G — UX Decisions Worth Logging

A few specific UX decisions made in this document worth recording in `bugs_and_decisions.md`:

```
[PLANNING PHASE] — Bottom navigation over hamburger menu
Decision: Three-pillar bottom nav + "more" tab.
Reason: Three pillars are parallel and equally important; bottom nav surfaces the structure rather than hiding it. Matches Android convention.

[PLANNING PHASE] — Body-as-hero principle
Decision: UI is overlay, sheet, or modal — never pushes the body offscreen unnecessarily.
Reason: The 3D body is the reason the app exists. UI exists to support it.

[PLANNING PHASE] — One question per screen for symptom dialogue
Decision: PRISM-001 dialogue is multi-screen, one question per screen, not a long form.
Reason: Reduces cognitive load. Each screen has one focus. Matches the AI prompt's design (one question per turn).

[PLANNING PHASE] — Permissions in-context
Decision: Camera permission requested when user enters AR mode, not at app launch.
Reason: Permission walls at launch correlate strongly with abandonment. Requesting in the moment of need with one-line context dramatically improves grant rates.

[PLANNING PHASE] — Dark mode as primary
Decision: Dark mode is the default and primary experience. Light mode is supported.
Reason: AR camera content is often dark; light UI fights it. Most medical professional tools use dark UI.

[PLANNING PHASE] — Visual identity deferred to iteration
Decision: Specific hex colors, typeface, iconography, and logo are NOT specified at planning stage.
Reason: Pre-specified visual design at the planning stage produces decisions that don't survive contact with real screens. Set constraints, iterate on specifics.
```

---

## Part H — How to Use This Document During UI Chats

When opening a chat to build a UI screen, paste this opener (which complements the Feature Build template):

```
Today we're building the UI for [SCREEN_NAME / FEATURE_ID].

Before writing code, please review:
1. The screen's specification in AnatomiQ_UX_And_Identity.md
2. The feature behavior in AnatomiQ_Features_Document.docx
3. The interaction patterns in Part D of the UX doc
4. The visual identity constraints in Part F — specifically the
   constraints that apply to this screen

Then propose:
- The screen's structure (component tree)
- The interaction flow (how the user moves through it)
- The accessibility considerations specific to this screen
- The empty/loading/error states this screen needs

Implement using minimal placeholder visual styling — clean and
neutral, following the typography and color constraints, but not
attempting final visual design. We'll iterate on visual design
separately.
```

---

*UX architecture and visual identity v1 · 2026 · structure and constraints, not specifications*
