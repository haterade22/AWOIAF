# RCA — Deep Review: ADOD Map Adoption (faction layer + distance-cache transcoder), 2026-07-14

**Scope reviewed:** the full uncommitted changeset — generated faction layer (31 cultures, 229
clans/lords/heroes, 29 kingdoms, 4 XSLTs, SPEC + generator), offline distance-cache transcoder +
in-game rebuild retarget, and the supporting validators/tests/docs.

**Review shape:** 8 agents (5 core + tooling-correctness + faction-data-semantics + adversarial
transcoder verifier). All findings below were re-verified against the source (or by executing the
real .NET XSLT / re-parsing the real bin) before being entered here — none were taken on agent
confidence alone. **All fixes landed in-session; every gate re-run green afterwards** (139 tool
unit tests incl. 24 new; 2,880 C# tests; audit + validator; real-XSLT Thenns check).

## Findings table

| # | Sev | Bug | Category | Why missed | Preventive action |
|---|-----|-----|----------|-----------|-------------------|
| 1 | HIGH | 21/229 generated lords cloned from vanilla `main_hero` (player shell, first battania "Lord" in vanilla lords.xml) → all-zero skills, minimal traits | generator template selection | First-match selection had no exclusion list for sentinel/system entries; author spot-checked one lord (empire) whose template happened to be fine | Filter (`main_hero` + skill-less shells) in `parse_lord_templates`; unit test pins it; lesson: **first-match template pickers need an explicit sentinel-exclusion list, and spot-checks must sample every selection bucket, not one** |
| 2 | HIGH | `clan_sturgia_5` (The Thenns) — SPEC-designated ruling clan of `Kingdom.freefolk` — derived into `Kingdom.battania` (its fiefs are mostly battania-tagged placeholders) | derivation vs SPEC cross-consistency | Generator never asserted "each kingdom's ruling clan lands in its own kingdom"; `audit_adod_map_refs.py` explicitly skipped vanilla clans because it couldn't apply XSLT — a **documented blind spot that nobody closed** | SPEC override; generator ruling-clan↔kingdom assert (fails the regen, not the game); audit now statically recovers spclans.xslt attribute overrides and ruler-checks all 37 kingdoms; unit tests pin both |
| 3 | HIGH | `Path.write_text` newline translation → generated XML/XSLT line endings depend on the OS the generator runs on | tooling I/O convention | `tools/README.md` "XML I/O convention" already mandates byte-explicit writes; author wrote the emitter without re-reading it — a scope gap, not a missing rule | `flush_outputs` writes explicit CRLF bytes; repeat of the `feedback_xml_tool_bom_io_convention` class — the tooling-correctness agent is the reliable net for this and should be launched for every script-writing changeset (it is now codified in the deep-review skill's adaptive-expansion rules) |
| 4 | HIGH | Overwrite-guard false negatives: hand-edited `.xml` under 400 bytes, or hand-edited `.xslt` with ≤10 templates, silently clobbered | tooling guard heuristics | Guard was written as size/substring heuristics under time pressure; heuristics reviewed by the author only | Structural guard (marker, empty-root placeholder, exact identity-stub shape); unit tests pin refusal + acceptance cases |
| 5 | HIGH | Non-atomic `--apply`: emitters wrote sequentially, so a late guard/validation failure left a partially-regenerated repo | tooling transactionality | The per-file guard created an illusion of safety; nobody asked "what state does a FAILED run leave?" | Staged in-memory rendering; all validations run in BOTH modes; disk writes only after everything passes. **Repeat-offender category**: same class as Codex review #39's P2-2 (`File.Replace` atomicity) on `RuntimeCacheRebuildService` — "what does a crash mid-write leave behind" must be asked of every writer, C# or Python |
| 6 | MED-HIGH | Transcoder `--apply` renamed the live cache to `.prev` BEFORE writing the new one (crash window with no cache at the live path) and left an UNVALIDATED file live if validation failed | tooling transactionality | Author modeled the `.prev` backup on `RuntimeCacheRebuildService` but inverted the ordering it exists to prevent | Temp-write → validate temp (now incl. bit-exact distance sampling, finding 10) → swap; same repeat-offender category as #5 |
| 7 | MED | `war_of_the_ring.json` `"enabled": false` is not authoritative — `DotsSettings.WarOfTheRingEnabled` (MCM, default `true`) wins whenever MCM is available; inert today only because the wars lists are also empty | config precedence | Author read the JSON loader but not `GetEffectiveEnabled()`'s settings-provider precedence — disabled one of two enable sources | MCM default flipped to `false` + hint text states the precedence. Lesson (extends the MCM-toggle-coverage rule): **disabling a feature via config requires enumerating every OTHER enable source that outranks it** |
| 8 | MED | SPEC `tier` hints on vanilla clans were dead data (spclans.xslt never emitted tier overrides) | dead config | Emitter symmetry gap: new clans consumed tier, vanilla clans didn't; no dead-key detection on the SPEC | `@tier` overrides now emitted for SPEC-explicit tiers (verified via real XSLT: Thenns tier 5) |
| 9 | MED | Name-pool exhaustion crashed with raw `IndexError` (suffix list bound); no dedupe on ruler names | generator fail-loud consistency | Exhaustion path never exercised (pools sized generously); ruler names had no guard because house names did | `SystemExit` with actionable message; ruler-name dedupe (SPEC collision raises, pool collision redraws); unit tests pin both |
| 10 | LOW×4 | Transcoder validation didn't check distance fidelity (B.7); `parse_map` regex would silently under-count non-self-closing `<Town>` (A.4); audit printed no load diagnostics (C); BodyProperties face-age drifted from assigned age (H2) | assorted | Cheap hardening skipped in the first pass | All four fixed: sampled bit-exact distance comparison in validation; Town-tag count assert; audit loaded-counts line; face age synced to assigned age |

**Not fixed (recorded, with reasons):**
- Efficiency MEDIUM — `.Count()` enumerations in `RuntimeCacheRebuildService`'s post-write
  verification logging: pre-existing code outside this changeset's diff (edit-scope discipline);
  non-critical path; noted as a follow-up candidate.
- M2 (informational) — `Kingdom.empire`'s owner is a DOTS-generated hero, deviating from TAOM's
  vanilla-hero-only precedent. Verified functionally safe (MBObjectManager presumed-object forward
  refs, same mechanism vanilla itself uses); House Targaryen has no vanilla clan, so the deviation
  is forced. Documented here as the deliberate exception.
- Data-flow side observation — ~25 dangling LOTR `equipmentsets/dots_equipment_sets_*`
  registrations in SubModule.xml: pre-dates this changeset (bootstrap leftover; engine skips
  missing files harmlessly). Cleanup candidate for the next SubModule pass.

## Root-cause patterns (2+ findings each)

1. **Writer transactionality asked-never (findings 5, 6).** Both new tools got per-item guards but
   no whole-run failure-state analysis, in a repo whose C# reviewer had already caught this exact
   class (Codex #39 P2-2). The question "what does a crash or a mid-run failure leave on disk?"
   belongs in the authoring checklist of every file-writing tool, and the deep-review
   tooling-correctness agent asks it reliably.
2. **Cross-representation consistency unasserted (findings 2, 8).** The SPEC, the derivation, and
   the XSLT emitters are three representations of one political design; the bugs lived in the seams
   (SPEC value never consumed; derivation contradicting SPEC intent). Fix pattern: asserts at the
   seam (ruling-clan↔kingdom), and audits that see through the transform layer (static XSLT
   override recovery).
3. **Author-blindness on generated-data quality (finding 1).** Structural validators (well-formed,
   refs resolve, counts match) all passed while 9% of lords were semantically hollow. Only a
   semantics-focused reviewer sampling per-bucket caught it.

## Why each core agent missed what it missed

- **Standards (Agent 1):** scope was the C# diff — findings 1–10 live in Python/XML/JSON. Correct
  scoping, nothing to change.
- **API compatibility (Agent 2):** verified deserializer contracts (which all passed) — semantic
  data quality (finding 1) and cross-representation seams (finding 2) are out of its lane.
- **Efficiency (Agent 3):** flagged the pre-existing `.Count()`; tooling atomicity/encoding aren't
  perf findings.
- **Completeness (Agent 4):** correctly flagged the missing tool unit tests (now written), which is
  the meta-gap behind findings 4/6/9.
- **Data flow (Agent 5):** caught finding 2 (by executing the real XSLT — the only agent to do so)
  and finding 7 (by tracing both enable sources). This review's evidence again supports the skill's
  claim that Agent 5 + the adaptive specialists are where the real bugs surface: all six
  must-fix findings came from Agent 5, the tooling agent, and the faction-semantics agent — zero
  from the three haiku core agents.

## Feedback memories to codify

- One genuine new generalization: **"disabling via one config source requires enumerating every
  other source that outranks it"** (finding 7) — worth a line in the MCM-toggle-coverage rule of
  the deep-review skill rather than a standalone memory.
- Findings 3, 5, 6 are repeat-offender categories already covered by existing memories/rules
  (`feedback_xml_tool_bom_io_convention`, Codex #39 atomicity) — the lesson is enforcement
  placement (tooling agent always-on for script changesets), which the deep-review skill already
  mandates; no new memory manufactured.
