# AnatomiQ — Operations & Planning Closeout

> **Purpose:** This document closes out the planning phase. It contains three pieces:
> (1) AI provider recommendation that resolves the last Open Question
> (2) Project risk register tracking non-technical risks that academic projects often fail on
> (3) Reusable prompt templates for opening common chat types in the Claude Project
>
> After this document, the planning is complete. Subsequent work is execution.

---

## Part A — AI Provider Decision

### A.1 The recommendation

**Primary: Anthropic Claude (Sonnet 4.6 for general use, Haiku 4.5 for high-volume narration).**

**Architect for swappability — CORE-006 should treat the AI provider as a configurable backend, not a hardcoded dependency.**

### A.2 Why Claude as primary

The AI provider for AnatomiQ is making one decision repeatedly: do I trust this model to follow a strict medical safety system prompt across thousands of calls, including adversarial ones? That's the question. Pricing is essentially irrelevant for an academic project at this scale (calculated below).

Three reasons Claude is the right primary:

**1. System prompt adherence**
Claude is specifically designed to weight system-prompt instructions strongly relative to user messages. This is the architectural feature AnatomiQ depends on most — the master prompt's hard rules need to hold even when users explicitly try to override them. Anthropic's safety research is centered on this property; OpenAI and Google care about it but have different priorities.

**2. Prompt caching with explicit cache control**
The AnatomiQ master system prompt + feature prompt totals ~2000 tokens of static content per call. Claude's caching system gives explicit `cache_control` breakpoints, meaning AnatomiQ can mark exactly where the static portion ends and pay 10% of the normal rate for that portion on every cache hit. With well-structured prompts, hit rates of 90%+ are realistic. This makes Claude's per-call cost competitive even though the sticker price is higher.

**3. Reasoning depth for cascade explanation**
Disease cascade narration and symptom dialogue both benefit from genuine reasoning, not just pattern completion. Claude's consistently high benchmarks on long-form reasoning and nuanced explanation matter when explaining how Type 2 Diabetes affects the kidneys *given the previous step about microvascular damage*.

### A.3 Cost analysis (the boring but reassuring math)

Per cascade narration (1 step):
- Input: master prompt + feature prompt + runtime context = ~2300 tokens
- Output: 1-2 sentences = ~40 tokens

| Model | Input rate | Output rate | Cost per step | Cost per cascade (11 steps) |
|---|---|---|---|---|
| Claude Sonnet 4.6 | $3/M | $15/M | $0.0076 | **$0.084** |
| Claude Sonnet 4.6 (cached) | $0.30/M | $15/M | $0.0014 | **$0.015** |
| Claude Haiku 4.5 | $1/M | $5/M | $0.0025 | **$0.028** |
| Claude Haiku 4.5 (cached) | $0.10/M | $5/M | $0.0005 | **$0.005** |
| GPT-5.4 | $2.50/M | $15/M | $0.0064 | $0.071 |
| GPT-5 Mini | $0.25/M | $2/M | $0.0007 | **$0.008** |
| Gemini 2.5 Flash-Lite | $0.10/M | $0.40/M | $0.00025 | **$0.003** |

Per Q&A response:
- Input: ~2500 tokens
- Output: ~200 tokens

| Model | Cost per Q&A | Cost per 100 Q&As |
|---|---|---|
| Claude Sonnet 4.6 | $0.011 | $1.10 |
| Claude Sonnet 4.6 (cached) | $0.004 | $0.40 |
| Claude Haiku 4.5 | $0.0035 | $0.35 |
| GPT-5 Mini | $0.001 | $0.10 |

Total cost for the entire academic demo phase (estimated 100 cascades + 200 Q&As + 50 symptom dialogues + 20 scan analyses):
- Claude Sonnet 4.6 with caching: **~$8-12 total**
- Claude Haiku 4.5 with caching: **~$2-3 total**
- GPT-5 Mini: **~$1-2 total**

**The cost difference between the most expensive and cheapest path is single-digit dollars across the entire project.** Pricing is not the deciding factor.

### A.4 Hybrid routing strategy

Even with Claude as primary, route different feature types to different model tiers:

| Feature | Recommended model | Reason |
|---|---|---|
| ATLAS-001 cascade narration | Haiku 4.5 | High volume, low complexity, short output |
| ATLAS-002 anatomy Q&A | Sonnet 4.6 | Educational depth matters, medium complexity |
| PRISM-001 symptom dialogue | Sonnet 4.6 | Highest safety bar, multi-turn reasoning |
| PRISM-004 scan analysis | Sonnet 4.6 | Vision + structured output + safety |
| PRISM-005 doctor explanation | Haiku 4.5 | Patient-friendly summaries, latency matters |
| PRISM-006 patient report | Sonnet 4.6 | Long-form structured generation |
| CADENCE-001 procedure narration | Haiku 4.5 | Short, factual, real-time |
| CADENCE-002 movement feedback | Haiku 4.5 | Real-time, brief, encouraging |

Practical effect: ~70% of calls go to Haiku 4.5 (cheap, fast), ~30% go to Sonnet 4.6 (where it matters). Total cost stays low while reasoning quality stays high where needed.

### A.5 Provider-agnostic architecture for CORE-006

Build CORE-006 with a model-agnostic interface so providers can be swapped without rewriting features. Three reasons this matters:

1. **Outage resilience** — if Anthropic's API has issues, fall back to OpenAI
2. **Cost optimization** — if pricing shifts, route accordingly
3. **Future-proofing** — small open-weight on-device models may eventually replace some external calls

```csharp
namespace AnatomiQ.AI
{
    public interface IAIProvider
    {
        Task<string> GenerateAsync(
            string systemPrompt,
            string userMessage,
            AICallType callType,
            CancellationToken ct);

        Task<T> GenerateStructuredAsync<T>(
            string systemPrompt,
            string userMessage,
            CancellationToken ct);

        bool SupportsStreaming { get; }
        bool SupportsVision { get; }
        bool SupportsPromptCaching { get; }
    }

    public class ClaudeProvider : IAIProvider { /* ... */ }
    public class OpenAIProvider : IAIProvider { /* ... */ }

    public class AIOrchestrator : MonoBehaviour
    {
        [SerializeField] private AIProviderType _primaryProvider;
        [SerializeField] private AIProviderType _fallbackProvider;

        private IAIProvider _primary;
        private IAIProvider _fallback;

        public async Task<string> GenerateAsync(...)
        {
            try { return await _primary.GenerateAsync(...); }
            catch (ProviderUnavailableException)
            {
                Debug.LogWarning("Primary provider unavailable, using fallback");
                return await _fallback.GenerateAsync(...);
            }
        }
    }
}
```

The provider type is set in app config, not hardcoded. Model selection within a provider (Haiku vs Sonnet) is driven by `AICallType`.

### A.6 Open-question status: CLOSED

Decision recorded. Add to bugs_and_decisions.md:

> **AI Provider Decision** — Claude as primary (Sonnet 4.6 + Haiku 4.5 routing). OpenAI GPT-5 Mini configured as fallback. Architecture is provider-agnostic via `IAIProvider` interface. Reason: Claude's system prompt adherence and explicit prompt caching are decisive for safety-critical PRISM features; cost difference is negligible at academic scale.

### A.7 Practical setup steps

1. Create Anthropic account at console.anthropic.com — verify with credit card (no charge until usage)
2. Generate API key, store in Unity's player settings or environment variable, **never commit to git**
3. Add to `.gitignore`: any file containing the API key
4. Set initial spending limit to $20/month in Anthropic console — protects against runaway bugs
5. (Recommended) Create OpenAI account and key as fallback configured but unused initially
6. Test both with a "hello world" call from Unity before building any features that depend on them

---

## Part B — Project Risk Register

### B.1 Why this exists separately from feature fallbacks

Per-feature fallbacks (CORE-007 territory) handle technical degradation: ARCore unavailable, API offline, framerate drops. Project-level risks are different — they're things that can derail the entire project regardless of how well the technical work goes. Academic projects often fail on these, not on technical issues.

### B.2 Risk format

Each risk has:
- **Probability** — Low / Medium / High
- **Impact** — Low / Medium / High / Critical
- **Early warning signs** — what to watch for that suggests this risk is materializing
- **Mitigation** — what to do now to reduce probability or impact
- **Contingency** — what to do if the risk materializes

### B.3 The register

#### R1 — Medical reviewer unavailable for cascade validation

**Probability:** Medium · **Impact:** High

Medical reviewer commitment falls through, gets too busy, or never finalizes. Phase 2 medical review checkpoint is missed. Demo proceeds with unreviewed cascades — clinicians watching the demo will catch errors.

**Early warning signs:** No reviewer identified by midpoint of Phase 1. Reviewer responses to outreach take >1 week. Reviewer hedges on time commitment.

**Mitigation:**
- Identify and confirm the reviewer before Phase 1 ends, not at Phase 2
- Multiple potential reviewers, not one — at least 2 backups (medical student peers count)
- Make the review easy: send pre-formatted JSON with "agree / suggest change" interface, not raw research papers
- Offer co-authorship on any resulting paper as motivation

**Contingency:** Reduce scope to fewer diseases (1-2 well-validated cascades > 5 unvalidated). For demo, frame uncovered diseases as "in development." If a clinician asks "who reviewed this?" answering honestly is far better than presenting unreviewed content as validated.

---

#### R2 — Test device hardware failure or loss

**Probability:** Low · **Impact:** Critical

Poco X5 Pro 5G is the only test device. If it's lost, broken, or stolen, the entire demonstration capability is gone. Replacement could take 1-2 weeks and a few hundred euros that may not be available.

**Early warning signs:** Device showing thermal issues, battery degradation, screen damage. Frequent need to charge or restart.

**Mitigation:**
- Use a basic case from day one — €5 protection against most drops
- Don't carry it in the same bag as a laptop or other heavy items
- Back up the development setup (Unity project + APK builds) to GitHub regularly
- Keep an APK build of the latest stable version on Google Drive or similar
- Identify a backup ARCore-compatible device available to borrow if needed (any modern Pixel, Samsung, or other Snapdragon phone)

**Contingency:** Use Unity Remote 5 or AR Simulation in editor as temporary substitute. ARCore is supported on most Android devices — borrow one for the final demo if the primary is unavailable. Cloud build services as a fallback for the build pipeline.

---

#### R3 — API provider disruption (rate limits, outage, deprecation)

**Probability:** Medium · **Impact:** Medium

Anthropic changes pricing dramatically, deprecates a model mid-project, applies stricter rate limits, or has an outage during the demo itself.

**Early warning signs:** Anthropic announcement emails about model deprecation. Rate limit errors appearing in development. Cost-per-call drifting upward.

**Mitigation:**
- Architect for swappable providers (Part A.5)
- Configure both Anthropic and OpenAI from day one even if only Anthropic is used
- Spending alerts at $5 and $15 in Anthropic console
- Cache common AI responses to ScriptableObjects for offline fallback per CORE-007

**Contingency:** Switch primary provider via configuration change. Fall back to ScriptableObject-cached responses for canned demo flow. The cascade narration fallback strings (per data schema spec) ensure cascades still work without any API.

---

#### R4 — Unity / AR Foundation / Inference Engine breaking change mid-project

**Probability:** Medium · **Impact:** Medium

A package update introduces a breaking API change, performance regression, or Android compatibility issue. Project breaks after an editor update.

**Early warning signs:** Editor update prompts appear in Unity Hub. Unity announces 6.x point releases. Package Manager shows pending updates.

**Mitigation:**
- **Lock package versions in `manifest.json` immediately after scaffold** — never use `latest`
- Don't update Unity editor mid-project unless a security fix requires it
- Read Unity 6.3 LTS changelogs before applying any patch updates
- Test patch updates on a branch before merging to main

**Contingency:** Roll back to last working commit. Pinned versions in Git make this simple. If a critical fix is in a newer version, accept a few days of porting work over a few weeks of debugging mysterious behavior.

---

#### R5 — Scope creep

**Probability:** **High** · **Impact:** High

The most likely failure mode for ambitious solo academic projects. Each "small addition" feels reasonable; cumulatively they push the project past available time. ATLAS-006 body pose overlay is the obvious risk vector — it's complex, optional, and seductive.

**Early warning signs:** Spending more than half a day deciding "what to build next" rather than building. Backlog of "would be cool" items growing faster than completed items. Working on Phase 3 features while Phase 2 is incomplete.

**Mitigation:**
- The Features Document defines scope. Anything not in there requires explicit decision to add
- Phase boundaries are gates — don't start Phase 2 until Phase 1 features are checked off in bugs_and_decisions.md
- ATLAS-006 explicitly framed as "may be cut" — if Phase 4 timeline is tight, it's the first thing to drop without guilt
- "No" is a productive answer to feature ideas during build phases. Add them to the deferred list and move on.

**Contingency:** Cut to MVP scope ruthlessly. Phase 1 + Phase 2 alone (working AR body model + disease cascade simulation with AI narration + medical review) is enough for a strong demo and a strong academic write-up. PRISM and CADENCE can be presented as "designed and architected, demonstrated at prototype level."

---

#### R6 — Major technical blocker on signature feature

**Probability:** Medium · **Impact:** High

CORE-005 Interconnectivity Engine, ATLAS-001 cascade simulation, or ATLAS-006 body pose overlay turns out to be much harder than anticipated. Days of work yield no progress. Discouragement compounds the problem.

**Early warning signs:** Same problem unsolved for 3+ days. Repeatedly hitting the same wall from different angles. Stack Overflow searches returning no useful results. Avoiding the project for several days.

**Mitigation:**
- Build the simplest possible version first, even if it's ugly. Hardcoded cascade with zero animation > beautifully designed unimplemented system
- Time-box exploration: if a problem isn't solving in 4 hours, take a break and look for alternative approaches
- Have a problem-buddy — peer or supervisor to talk through stuck problems with
- Build CORE-005 with worked example data (T2 Diabetes from data schemas doc) as the test case from day one

**Contingency:** Reduce feature complexity. If full cascade animation is too hard, ship a simpler step-through with manual advance. If interconnectivity graph traversal is too complex, ship a hardcoded sequence per disease. Working simple > broken sophisticated.

---

#### R7 — Time overrun affecting academic deadlines

**Probability:** Medium · **Impact:** High

Course deliverable deadlines arrive with the project not in demo-ready state.

**Early warning signs:** Less than 1 month remaining and Phase 2 still in progress. Demo script not yet written. No video footage captured. Documentation hasn't started.

**Mitigation:**
- Set personal milestones at 50%, 75%, 90% of available time — not at the deadline
- Demo video can be recorded incrementally throughout development, not at the end
- Documentation written *during* development (the existing AnatomiQ docs are already this) — not after
- Have a "submit-able state" by 75% of the timeline, polish in remaining time

**Contingency:** Cut Phase 4 entirely. Phase 3 features that aren't complete: present the architecture and partial implementation, don't try to fake completeness. Academic graders generally prefer honest scope reduction over feature lists that don't actually work.

---

#### R8 — 3D model legal or technical issue

**Probability:** Low · **Impact:** Medium

Z-Anatomy or Open3DModel licensing changes. Asset preparation pipeline produces unusable meshes after Blender work. License attribution forgotten in build, potentially blocking academic submission if reviewer is strict.

**Early warning signs:** Z-Anatomy / Open3DModel project websites going down or changing terms. Mesh import errors that don't resolve. Forgetting which structure has which FMA ID after Blender work.

**Mitigation:**
- Download a complete copy of Z-Anatomy at start and archive it — don't rely on the source remaining available
- Include attribution in app credits screen from day one, not at the end
- Document the asset preparation pipeline as you do it (not from memory afterward)
- Save Blender source files alongside exported glTF — so re-export is possible without redoing decimation

**Contingency:** Fall back to BodyParts3D source data directly (more raw but stable). For attribution, simple text screen suffices — no special design needed.

---

#### R9 — Health / personal life disruption

**Probability:** Medium · **Impact:** Variable

Illness, family emergency, mental health challenges, or other life events that significantly disrupt 2+ weeks of project time. Common across the lifespan of any multi-month project.

**Early warning signs:** Often only visible in retrospect. Sustained low energy, sleep issues, avoiding the project repeatedly.

**Mitigation:**
- Front-load progress when possible — buffer time matters more than evenly-paced effort
- The phase structure is a buffer against this — Phase 1 + Phase 2 is enough for a credible submission, so getting through them early matters more than completing all four phases evenly
- Communicate early with course supervisor if disruption happens — academic systems are often more accommodating than students assume

**Contingency:** Use available extension mechanisms in your academic system. Submit reduced scope honestly. Health is more important than a project.

---

#### R10 — Demo day technical failure

**Probability:** Medium · **Impact:** High

Final demo presentation: the device crashes, AR session fails to start, network is unreliable in the demo room, or some other live failure during the most important moment.

**Early warning signs:** Hadn't tested in the actual demo environment. Demo flow not rehearsed end-to-end. No backup video footage.

**Mitigation:**
- **Pre-record a video walkthrough** of the entire demo flow, polished, with narration. Show this if anything fails live.
- Test the demo build in the actual demo environment (or one similar) before demo day
- Use offline mode (cached responses, pre-baked cascades) for the demo — don't depend on conference Wi-Fi
- Bring a charged backup device if possible
- Practice the demo 3+ times end-to-end the day before

**Contingency:** "Let me show you the recorded version while I describe what's happening live" — present the video. This is fine. Many professional product demos do exactly this.

### B.4 Risk register summary

| Risk | Probability | Impact | Status |
|---|---|---|---|
| R1 Medical reviewer | Medium | High | Open — needs reviewer identified by Phase 1 end |
| R2 Hardware failure | Low | Critical | Mitigated — backup strategy planned |
| R3 API disruption | Medium | Medium | Mitigated — provider-agnostic architecture |
| R4 Package breakage | Medium | Medium | Mitigated — version pinning |
| R5 Scope creep | **High** | High | **Active vigilance required** |
| R6 Technical blocker | Medium | High | Strategy: build simple first |
| R7 Time overrun | Medium | High | Mitigated — phased milestones |
| R8 3D model issue | Low | Medium | Mitigated — local archive |
| R9 Health disruption | Medium | Variable | Mitigated — buffer in early phases |
| R10 Demo failure | Medium | High | Mitigated — pre-recorded backup |

The two risks worth staying most alert to throughout: **R5 (scope creep) and R1 (medical reviewer)**. Most of the others are bounded by the existing architecture and planning.

---

## Part C — Claude Projects Prompt Templates

These templates are for opening common chat types in the Claude Project. Paste the relevant template at the start of a new chat to immediately set the right context. Saves re-typing the framing every time and ensures every chat starts in the right mode.

### C.1 Scaffold chat opener

Use this when starting the very first development chat — establishing the Unity project foundation before any features exist.

```
This is the AnatomiQ scaffolding session. Today we're building the
foundational codebase only — no features yet, only structure.

Before writing any code, please verify the current state of these
packages with a web search:

- Unity 6.3 LTS current point release
- AR Foundation latest stable for Unity 6.3
- Google ARCore XR Plugin current version
- Unity Inference Engine (com.unity.ai.inference) current version
- Newtonsoft JSON for Unity status

If any has moved since the planning documents were written, flag it
clearly and adjust recommendations.

Today's deliverables:
1. Folder structure under Assets/_AnatomiQ/ matching the project plan
2. Base namespace stubs (AnatomiQ.Core, .AR, .Anatomy, .AI, .UI, .Data)
3. CORE-007 Fallback Manager shell — empty class with the AppState enum
   and the monitoring loop scaffold, no logic yet
4. Event bus pattern for inter-system communication
5. Empty ScriptableObject schema definitions matching the data schema doc
6. .gitignore appropriate for Unity 6 + Android development
7. URP project settings configured per the performance budget doc

Do not implement any feature logic. Just scaffolding.
```

### C.2 Feature build chat opener

Use this when starting a chat to build a specific feature.

```
Today we're building [FEATURE_ID]: [FEATURE_NAME].

Before writing code:
1. Look up [FEATURE_ID] in the Features Document and confirm the
   description, dependencies, AI role, AR role, and fallback behavior.
2. Confirm all dependencies are already implemented and tested. If any
   dependency is unmet, stop and tell me — we'll build the dependency first.
3. Check the bugs_and_decisions.md for any prior decisions or bugs
   relevant to this feature.
4. Note any package APIs you plan to use that may have changed since
   the docs were written — verify with a quick search if uncertain.

Then propose an implementation plan covering:
- Class structure and namespaces
- Public API surface this feature exposes (events, methods)
- Integration points with dependencies
- How fallback behavior is implemented
- What unit tests should be written

I'll review the plan before you start implementing. We build incrementally
and test after each meaningful chunk, not all at once.
```

### C.3 Debug chat opener

Use this when something is broken and the focus is fixing it.

```
I'm debugging an issue with [FEATURE_ID]: [BRIEF_PROBLEM_DESCRIPTION].

What I've observed:
- [observation 1]
- [observation 2]

What I expected:
- [expected behavior]

What I've already tried:
- [attempted fix 1] — outcome
- [attempted fix 2] — outcome

Relevant files attached / pasted below. Please work through this
systematically — don't jump to a fix. Help me understand what's
actually happening first, then we decide on a fix together.
```

### C.4 Performance review chat opener

Use this every few features to check the performance budget.

```
Performance review session for AnatomiQ. We've completed:
- [list of recently completed features]

I've run the on-device test scenarios from the performance budget
document. Results:
- [scenario 1]: [actual FPS / RAM / observations]
- [scenario 2]: [actual FPS / RAM / observations]
- ...

Compare these against the budget targets in
AnatomiQ_Performance_And_Models.md. For any metric that's drifted
toward or past its budget, identify the most likely cause and propose
specific optimizations.

Don't suggest premature optimization for metrics still well within
budget. Focus only on actual or imminent issues.
```

### C.5 Weekly review chat opener

Use this once per week to take stock of progress.

```
Weekly review for AnatomiQ.

This week I worked on:
- [completed items]
- [in-progress items]
- [blockers encountered]

Going into next week:
- [planned items]

Help me with:
1. Reality check — am I on track for the academic timeline?
2. Risk register check — has anything in the project risk register
   shifted? Any new risks?
3. Scope check — am I building what's in the Features Document, or
   has scope crept?
4. What's the most leverage-producing thing I should work on next week,
   given what's done and what's blocked?

Be honest, including about pacing. Tell me directly if you think I'm
falling behind, building things outside scope, or avoiding hard work
on the hard features.
```

### C.6 End-of-feature wrap-up

Use this at the end of any feature build chat to capture learnings.

```
[FEATURE_ID]: [FEATURE_NAME] is now built and tested on device.

Generate the bugs_and_decisions.md update for this session, in the
existing format:

1. Update the feature status table to mark this feature complete
   with today's date and ✅ status
2. Add any new architectural decisions made during this build to the
   "Architecture & Technical Decisions" section
3. Add any bugs solved to the "Bugs & Fixes" section in the
   feature → symptom → cause → fix format
4. Add any performance observations to "Performance Notes"
5. Add any unexpected dependency interactions to "Dependency Discoveries"
6. If you noticed a recurring pattern across this and previous bugs/
   features, add it to "Patterns I Keep Hitting"
7. Open questions newly discovered during this build, added to the
   Open Questions table

Output as markdown ready to paste into the existing file.
```

### C.7 Medical review request template

Not for Claude — this is for sending to your medical reviewer once identified.

```
Subject: AnatomiQ — Medical accuracy review request for cascade simulation

Hi [Name],

Thank you for agreeing to review the medical content for AnatomiQ.
The deliverable is a small set of disease cascade sequences — animated
simulations of how a condition propagates through the body — used in
an educational AR anatomy app.

Attached:
- 1-page project overview (what AnatomiQ is, who it's for)
- [N] cascade JSON files, each containing:
  · Disease name and description
  · Ordered sequence of physiological steps
  · Each step has a target organ and a fallback narration sentence

What I need from you:
- Confirmation that each cascade sequence is medically accurate
- Suggestions for any step that should be added, removed, or reworded
- Confirmation that the fallback narration text is accurate
- Acknowledgement on the citation list

Estimated time: 30-60 minutes per disease. Available formats: edit
the JSON directly, mark up a printed copy, or video call to walk
through together.

Happy to acknowledge you in the project documentation and any resulting
academic write-up.

Thank you,
[Your name]
```

---

## Part D — Updates to bugs_and_decisions.md

These are entries to add to the existing document now that planning is complete:

### D.1 Open questions — close out

```
| Which 3D model source | RESOLVED — Z-Anatomy / Open3DModel (CC BY-SA), see Performance & Models doc |
| Medical reviewer contact | OPEN — must be identified by Phase 1 end |
| AI provider choice | RESOLVED — Claude primary (Sonnet 4.6 + Haiku 4.5 routing), OpenAI fallback, provider-agnostic architecture in CORE-006 |
```

### D.2 New decisions to log

```
[PLANNING PHASE] — 3D model source decision
Decision: Z-Anatomy / Open3DModel for academic build, BioDigital deferred to commercial phase.
Reason: Free, university-backed, FMA-IDed, medically credible, CC BY-SA license suitable for academic use.
Constraint: Attribution required in credits screen. ShareAlike clause is constraint for commercial use later but not academic.
Asset preparation: 8-step pipeline in Performance & Models doc. Estimated 2-3 weeks Blender work.

[PLANNING PHASE] — AI provider decision
Decision: Anthropic Claude as primary (Sonnet 4.6 for high-stakes calls, Haiku 4.5 for high-volume narration). OpenAI GPT-5 Mini configured as fallback.
Reason: Claude's system prompt adherence is decisive for medical safety in PRISM. Explicit prompt caching with cache_control breakpoints gives 90% discount on static system prompts. Cost difference at academic scale is negligible (~$10-15 total estimated).
Architecture: CORE-006 implements provider-agnostic IAIProvider interface. Provider type set in config, not hardcoded.

[PLANNING PHASE] — Performance budget locked
Decision: Hard performance limits set per the Performance & Models doc. CORE-007 Fallback Manager monitors all metrics and triggers degradation at thresholds.
Soft RAM ceiling: 1.4 GB. Frame budget: 33ms at 30 FPS. Polygon budget: 1M total, 2 layers max visible.
Reason: Snapdragon 778G constraints require explicit limits to prevent late-stage scope problems.

[PLANNING PHASE] — Schema v1.1 update planned
Pending change: Add optional fmaId field to OrganAsset schema.
Reason: Z-Anatomy meshes use FMA ontology IDs. Aligning organId metadata with these enables cross-referencing with other anatomy databases. Future-proofs the data layer.
Status: Deferred to first scaffold update.
```

---

*Operations & Planning Closeout v1 · 2026 · Final document of the planning phase*
