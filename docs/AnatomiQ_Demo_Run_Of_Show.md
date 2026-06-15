# AnatomiQ — Demo Run-of-Show v0

> **Purpose:** Defines what the AnatomiQ academic demo will actually show. This document exists *now*, not at the end of Phase 4, because it shapes scope. Every feature decision during development should be checked against this script: "Does this feature serve the demo, or is it scope creep?"
>
> **Status:** v0 — first draft, will evolve. The structure (timing, story arc, fallback strategy) is more durable than the specific words.
>
> **Audience for this document:** the developer, before each demo rehearsal. Not for the demo audience.

---

## Part A — Demo Strategy

### A.1 Core principle: depth over breadth

A focused 4–5 minute demo of the signature feature is more impressive than a 12-minute feature tour. Graders, jurors, and audiences have finite attention. The first 90 seconds determine whether the audience leans in or checks their phone.

**The demo's only job:** make the audience think "I haven't seen this before, and it's medically meaningful."

If a feature doesn't serve that, it doesn't go in the demo. Features can exist in the app without being demoed.

### A.2 What gets demoed vs. what gets mentioned

| Demoed live (interactive) | Mentioned but not shown |
|---|---|
| Disease cascade simulation (T2D) | Cadence procedure training (just describe) |
| Body Interconnectivity (one example) | Scan analysis (show static screenshot if asked) |
| AI Q&A on a selected organ | Symptom dialogue full flow (mention as designed) |
| Layer toggle (briefly) | Time slider for cascade (mention if time) |
| AR placement (10 seconds, then proceed) | Performance scoring, multi-disease library |

**Why this split:** the live demo is where attention is highest. Use it for the best 4 minutes of content. Everything else gets mentioned in answers to questions or shown in supporting materials.

### A.3 Demo formats to prepare

Three formats, in order of priority:

**1. Live device demo (primary)** — 4–5 minutes interactive, the real thing
**2. Pre-recorded video walkthrough (backup)** — 4–5 minutes polished, exact same content as live, used if anything fails
**3. Static screenshots (fallback's fallback)** — 6–8 key moments captured as images, used if even the video doesn't play

Prepare all three. Test all three. Update all three when the app changes.

### A.4 Audience modes to prepare for

The same demo will be given to different audiences. Not three different demos — one demo with three audience-tailored intros and Q&A pivots.

| Audience | Opening framing | Likely questions |
|---|---|---|
| Academic graders | "This is my Research Techniques final project. I'll show the signature feature, then I'm happy to discuss the architecture, the medical content validation, or the academic writeup." | Methodology, validation, why these tools, what's novel |
| Medical professionals | "This is an educational tool for explaining how diseases affect the body as a system. Can you tell me if what you see here is medically credible?" | Accuracy, clinical use cases, where could this go wrong |
| Technical audience | "This is an AR + AI mobile app I built solo. The signature is a knowledge-graph-driven cascade animation with on-the-fly AI narration." | How does the AI work, what's the architecture, performance |
| General / non-technical | "This is an app that lets you see how diseases actually affect the body — not just as text, but as an animated cascade across the whole body." | What can it teach me, who's it for, will it be available |

The middle 4 minutes don't change. The opening sentence and the questions you anticipate are what flex.

---

## Part B — The Demo Script

### B.1 Total runtime: ~4:30 active demo + Q&A

```
00:00 ─ Cold open & first impression       [30s]
00:30 ─ AR placement, brief                 [20s]
00:50 ─ Layer system, brief                 [20s]
01:10 ─ Disease selection                   [15s]
01:25 ─ Cascade simulation begins           [─]
       ─ Early stage                        [40s]
       ─ Intermediate stage                 [40s]
       ─ Advanced stage                     [35s]
03:30 ─ Interconnectivity reveal            [40s]
04:10 ─ Q&A demo                            [25s]
04:35 ─ Outro & invite questions            [10s]
04:45 ─ Q&A from audience                   [open]
```

### B.2 Section-by-section script

The exact words evolve. The beats and the demonstrated content are stable. Each section below has: stage direction, on-device action, and example narration.

---

#### **00:00 — Cold open & first impression** [30 seconds]

**Stage direction:** Phone in hand, ready. Don't show the app yet. Make eye contact with the audience.

**Narration (example):**
> "Medical anatomy education today still relies on textbook diagrams and static 3D models. They show you what's there, but they don't show you what *happens* — how a disease propagates through the body as a system. That's what AnatomiQ does."
>
> *[Open the app]*
>
> "I'll show you in about four minutes."

**Why this opening:** establishes the gap, sets up the differentiator, makes a time commitment. Doesn't oversell. Audience knows what they're getting.

**On-screen at end of section:** ATLAS Home with body model in 3D Viewer mode, neutral state.

---

#### **00:30 — AR placement, brief** [20 seconds]

**Stage direction:** Tap "AR Mode," point at the table or the floor in front of you. Let plane detection happen for ~5 seconds.

**Narration:**
> "AnatomiQ runs in AR — so the body sits in real space."
>
> *[place model on surface]*
>
> "Or floats in space, which is what I'll use today since it's faster."

**Why this section is short:** AR is impressive for the first 10 seconds and then becomes invisible. Don't over-invest. The cascade is what's actually novel.

**On-screen at end:** Body model visible in AR or AR Space mode, ~80% of the screen, oriented toward the audience.

**Failure mode:** if AR doesn't track the surface, switch to 3D Viewer mode silently and continue. Skip the AR sentence. The cascade is the demo, not the AR.

---

#### **00:50 — Layer system, brief** [20 seconds]

**Stage direction:** Open the Layer Toggle sheet. Toggle skeletal, then add vascular, then dismiss.

**Narration:**
> "Six anatomical layers — skeletal, muscular, vascular, nervous, lymphatic, organs. Toggle independently or in combinations."
>
> *[skeletal on, then add vascular]*
>
> "But this isn't what makes AnatomiQ different. Other apps do this. Here's what they don't."

**Why this transition:** sets up the cascade reveal. Audience just saw what AnatomiQ shares with existing apps. Now they're primed for the differentiator.

---

#### **01:10 — Disease selection** [15 seconds]

**Stage direction:** Tap "Disease," scroll briefly to show there are options, tap "Type 2 Diabetes."

**Narration:**
> "Pick a disease."
>
> *[open Disease Picker]*
>
> "I'll pick Type 2 Diabetes — common, multisystem, instructive."
>
> *[tap Type 2 Diabetes]*

**Why this works:** the picker reveals AnatomiQ supports multiple diseases without listing them all. Audience sees scope without the demo turning into a feature tour.

---

#### **01:25–03:30 — Cascade simulation** [~2 minutes]

This is the heart of the demo. The cascade plays automatically; the narration runs alongside the visual animation. **Speak less during this section** — the app does the talking via AI narration. The developer's job is to highlight what's happening at the system level, not duplicate the AI's voice.

**Stage direction:** cascade auto-plays. Hold the phone steady so the audience can see the animation. Pause briefly between stages to let the audience absorb.

**During Early stage [01:25–02:05, ~40s]:**

The AI narrates each step (~5 seconds each). After steps 0–1 finish, optionally interject:

> "Notice — this isn't a static diagram. The pancreas activates first. Then peripheral tissues become insulin resistant. Then blood glucose rises. Each step triggers the next. The AI narrates them as they happen, generated against medically reviewed content."

After step 3 (kidney glomerular hyperfiltration):

> "The kidneys are already involved, even at this early stage."

**During Intermediate stage [02:05–02:45, ~40s]:**

After step 5 (microvascular damage):

> "Now the cascade spreads systemically. Same disease, same patient — the consequences appear across multiple organ systems."

After step 6 (retinopathy) and step 7 (peripheral neuropathy):

> "This is where text-based education stops. You can read that diabetes affects the eyes and the feet. You don't *see* how it gets there from the pancreas."

**During Advanced stage [02:45–03:30, ~45s]:**

Let the cascade play. The advanced stages are visually striking — kidney shrinking, vessels thickening, retinal vessels rupturing. Don't talk over them. Maybe one sentence:

> "And this is where the disease ends up if it isn't caught."

When the cascade completes, the body model holds in its end-state. Pause for 2–3 seconds before continuing.

---

#### **03:30 — Interconnectivity reveal** [40 seconds]

**Stage direction:** The cascade has finished. Tap an organ that was affected — the kidney is a strong choice because it both received and propagated effects.

**Narration:**
> "Now — the cascade you just saw isn't a hard-coded animation. It's generated from a knowledge graph."
>
> *[tap the kidney]*
>
> "Tap any organ and you can see what affects it, and what it affects."
>
> *[connection lines appear]*
>
> "The kidneys connect to blood pressure, to bone marrow via erythropoietin, to the parathyroid system. So if I picked Hypertension instead — different cascade, but the kidney is still on the path. Same graph, different traversal."

**Why this matters:** the audience just watched a beautiful animation. This sentence reveals it isn't just an animation — it's a genuine knowledge representation. Distinguishes AnatomiQ from "well-animated 3D anatomy."

**On-screen:** kidney highlighted, connections visible to several other organs, info card describing one connection.

---

#### **04:10 — AI Q&A demo** [25 seconds]

**Stage direction:** Slide up the Q&A sheet. Type one question.

**Narration:**
> "And the AI assistant has full context of what's currently selected and what's on screen."

Type the question:
> "Why does the kidney get involved in diabetes?"

**Stage direction:** AI streams a response. Read along briefly so the audience knows it's working.

**Narration after response:**
> "It knows the kidney is selected. It knows Type 2 Diabetes was the active simulation. The answer is contextual to what I'm looking at."

---

#### **04:35 — Outro & invite questions** [10 seconds]

**Stage direction:** Set phone down on the table — show that the demo is over. Make eye contact again.

**Narration:**
> "That's the core of AnatomiQ. The signature feature — the Body Interconnectivity Engine — is what I haven't seen in any anatomy app today. There's more in the project: a clinical assistant module, surgical training tools — but I won't run through them unless you want to see something specific. Happy to discuss the architecture, the medical content validation, or anything else."

**Why this ending:** confident handoff to Q&A. Doesn't trail off. Invites the conversation that grading or judging actually depends on.

---

### B.3 Possible questions and prepared answers

These aren't scripted answers to memorize — they're talking points the developer should be ready to articulate.

**"How is this different from Complete Anatomy or BioDigital?"**
> "Both are excellent for static anatomy reference — looking up structures, learning isolated systems. AnatomiQ adds two things: the Interconnectivity Engine that traces effects across the whole body, and AI narration that's contextual to what you're looking at. They show what's there. AnatomiQ shows what *happens*."

**"Was the medical content reviewed?"**
> "Yes — I worked with [reviewer name and credential] who reviewed the cascade sequences and narration text for the three diseases included. The cascades are sourced from peer-reviewed literature — primary sources include StatPearls, the Swedish Diabetes Register cohort studies, and KDIGO 2024 guidelines for CKD."

**"Is this safe to use clinically? Does it diagnose?"**
> "It's an educational tool, not a diagnostic one. The PRISM module does include a symptom checker, but it never diagnoses. The system prompts explicitly forbid that. Every output is framed as educational with a referral to a healthcare provider. There are also hard rules for emergency symptoms — if a user describes chest pain or stroke symptoms, the dialogue immediately stops and redirects to emergency services."

**"What's the AI doing on-device vs. in the cloud?"**
> "Real-time visual stuff — body pose estimation for the surgical training, body detection — runs on-device using Unity's Inference Engine with ONNX models. Anything reasoning-heavy — Q&A, narration, symptom dialogue — runs via Anthropic's Claude API. Architecture is provider-agnostic so the LLM provider can be swapped without rewriting features."

**"What happens when AR doesn't work or the network is down?"**
> "Three fallback layers. If ARCore isn't available — older device, no camera permission — it drops to 3D Viewer mode silently. If the AI API is unreachable, the cascade plays with pre-baked narration that's medically reviewed. If performance drops below 30 fps, the renderer auto-reduces detail. The principle is: AR and AI are enhancements, not requirements. The 3D anatomy exploration always works."

**"What's the next step? Is this a product?"**
> "It's an academic project right now. The architecture is built so the three pillars could become separate products eventually — AnatomiQ Learn for students, AnatomiQ Clinic for doctors, AnatomiQ Surgical for procedure training. But that's a multi-year path with regulatory complexity for the clinical side. For now, my goal is to ship a strong academic project."

**"How long did this take? Did you have a team?"**
> "Solo project, [N months] of part-time work alongside coursework. The planning phase took about [N weeks] — I have full architectural documentation if you want to see how it was structured. Most of the time was the cascade animation engine and getting the on-device performance right."

**"What was the hardest part?"**
> "The Interconnectivity Engine itself — designing a data structure that can drive both cascade animations and the explorer view, while staying medically accurate. And keeping the AR session stable while running AI inference on-device. Mid-range mobile hardware leaves a tight performance budget."

**"Show me the symptom checker / scan analysis / surgical training."**
> *(If time permits, switch to PRISM tab and run through a brief flow. If short on time:)* "I'll show you the highlights — happy to do a longer demo afterward if you want."

---

## Part C — Failure Mode Playbook

What to do when something goes wrong during the live demo. Memorize the response, not the exact words.

### C.1 Cascade fails to start

**Symptom:** Disease selected, no animation begins.
**Cause possibilities:** AI API timeout (cascade waits for first narration), data not loaded, animation thread frozen.
**Response:** "Let me show you the recorded version while I describe what's happening." → switch to pre-recorded video.

### C.2 AR placement fails

**Symptom:** Surface won't lock; model doesn't appear.
**Response:** Tap mode switcher → "I'll just use space mode for the demo." Continue normally. Do not apologize, do not draw attention to the AR issue. The demo isn't about AR.

### C.3 Network is unavailable

**Symptom:** Cascade plays but AI narration fails (silence or fallback text appears).
**Response:** "The cascade has medically-reviewed fallback narration that plays when the AI is offline — that's what you're seeing now. With network, it'd be live AI narration." Continue normally. **This actually demonstrates the fallback architecture, which is a positive.**

### C.4 Q&A response is broken or wrong

**Symptom:** AI returns garbled response, error, or factually wrong content.
**Response:** Move on quickly. "AI's having a moment — let me skip to the close." Don't try to recover the same question.

### C.5 App crashes

**Symptom:** App closes unexpectedly.
**Response:** Don't try to relaunch in front of the audience. "Let me show you the recorded version." → switch to pre-recorded video. Re-launching looks worse than acknowledging and pivoting.

### C.6 Phone runs out of battery / dies

**Response:** "I have a backup video — let me play that." → laptop shows the recording. Always have a charged backup mechanism (laptop, second phone, or USB-C battery pack).

### C.7 Device gets warm / thermal throttle visible

**Symptom:** "Device is getting warm" overlay appears mid-demo.
**Response:** This is the planned UX — don't apologize. "AnatomiQ has thermal management built in — it'll quietly reduce rendering quality to keep things smooth." Continue. **This too is a positive: it demonstrates real engineering.**

---

## Part D — Pre-Demo Checklist

Run this 24 hours before the actual demo, not 30 minutes before.

### D.1 Device

- [ ] Phone charged to 100%
- [ ] Battery saver / power saving OFF (won't throttle during demo)
- [ ] Brightness set to ~70% (visible but not maximally draining)
- [ ] Do Not Disturb / Focus Mode ON (no notifications)
- [ ] Latest stable build installed and verified
- [ ] Test cascade plays end-to-end without crashing
- [ ] Test AI Q&A returns a response
- [ ] Test AR placement works in similar lighting to the demo venue
- [ ] Backup phone or laptop available with same content

### D.2 Network / API

- [ ] Anthropic API key valid, sufficient credits
- [ ] Recent API call test successful within last 24 hours
- [ ] App's offline fallback verified working (force airplane mode, run cascade)
- [ ] If venue Wi-Fi unreliable: phone tethered to mobile hotspot as backup

### D.3 Pre-recorded backup

- [ ] Video recorded with the same flow as the live demo
- [ ] Video runtime matches live demo within 30 seconds
- [ ] Video has voice-over or captions
- [ ] Video is on the laptop, on the phone, AND on cloud backup
- [ ] Video plays without internet
- [ ] Static screenshots of 6–8 key moments saved to phone

### D.4 Demo content state

- [ ] Test cascade is Type 2 Diabetes (the most polished one)
- [ ] Q&A history is empty (no prior conversation visible)
- [ ] No personal data, debug overlays, or development artifacts visible
- [ ] App opens to ATLAS Home, not the last visited tab
- [ ] Audience level set to "General" by default

### D.5 The developer

- [ ] Full demo rehearsed end-to-end at least 3 times the day before
- [ ] Q&A answers practiced out loud (not just thought through)
- [ ] Outfit doesn't reflect glare on the screen (matte clothes preferred)
- [ ] Water nearby
- [ ] Time-of-day caffeine alignment with the demo time
- [ ] Quick visit to the demo venue ahead of time if possible — for lighting, table, projection setup

---

## Part E — Post-Demo

### E.1 Immediate (within 15 minutes)

- Note any failures or near-failures while memory is fresh
- Note questions you struggled to answer well — these become areas to strengthen
- Note audience reactions — what made them lean in, what lost them

### E.2 Same day

- Update `bugs_and_decisions.md` with anything that broke or revealed a gap
- Update this document if any section consistently runs long or short
- Save audience questions you didn't expect — they often surface real product issues

### E.3 If multiple demos

- After 2–3 demos, the script will tighten naturally
- Some sections will compress (you'll know them too well to spend full time on them)
- Some sections will expand (audience reactions will reveal what deserves more emphasis)
- v0 of this document is the starting point, not the final form

---

## Part F — How This Document Shapes the Build

The most important function of this document: it's a scope filter.

Every feature decision should be checked: **does this serve the demo, or am I building it for completeness?**

- A feature that's in the demo → P0 or P1, must work flawlessly
- A feature that's mentioned in Q&A → P1 or P2, must work but doesn't need polish
- A feature that's not in the demo or the Q&A talking points → P2 or P3, ask whether it should exist at all

Apply this filter especially:
- Before starting work on any P2 or P3 feature
- When deciding whether to polish or move on
- When tempted to add "one more thing"
- During scope creep moments — Risk R5 from the planning closeout

The demo doesn't need every feature. The features need to serve the demo.

---

*Demo run-of-show v0 · 2026 · update after every rehearsal and every actual demo · v0 is a draft, not a contract*
