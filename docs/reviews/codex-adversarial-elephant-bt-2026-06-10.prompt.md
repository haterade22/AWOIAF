Adversarial code review: DOTS war-elephant AI behavior tree + cooldown attack system. Bannerlord 1.4.5 total-conversion mod (.NET Framework 4.7.2). Repo root is the current working directory.

WHAT THIS IS:
A NEW behavior-tree feature. An AI-ridden war elephant attacks via a per-agent behavior tree that mirrors the proven warg pattern. The 2026-06-10 "phase 1.5" rework replaced ADOD_Beasts' random per-tick trample roll with deterministic cooldowns. The intended logic: enemy in front within range -> fire TRAMPLE if off its 10s cooldown -> else fire a LEFT or RIGHT tusk swing (chosen by the enemy's bearing) if off its 4s cooldown -> else idle, letting the engine's normal mount AI (rider cavalry AI + native charge) continue. The BT only layers attacks on top of normal movement.

YOUR JOB:
Find REAL bugs with file:line evidence. This code already passed a 5-agent Claude deep-review (0 HIGH/MED) and a 13-agent adversarial workflow, so the easy findings are gone. Look harder -- BT control-flow edge cases, cross-node state sharing, the cooldown timeline, lifecycle/isolation, and engine-interaction correctness. Confirm or DISPUTE each Known Suspect below. Do not flag warg-matching patterns as bugs (the warg is the proven precedent). Do not invent findings -- "no findings in this section" is a valid, valuable answer.

READ FIRST (do not skip -- the BT semantics are non-obvious):
- docs/features/elephant.md, the "Behavior tree (AI attacks) -- 2026-06-10" section (tree shape, cooldown model, verified clip mapping, the "Review notes" block listing accepted behaviors).
- Main/BehaviorTrees/BehaviorTreesCore.cs and Main/BehaviorTrees/Nodes/BehaviorTreesNodes.cs -- the DOTS-inlined behavior-tree library. The blackboard property-copy (CopyAllInterfacesProperties) and the Selector/Sequence HandleExecute state machines are LOAD-BEARING. Read them before reasoning about control flow or state sharing.
- Main/Features/Warg/WargBehaviorTree.cs, WargMissionBehavior.cs, and Main/Features/Warg/BehaviorTreeElements/ -- the proven reference this feature mirrors.

BT LIBRARY SEMANTICS (established facts -- use these, do not re-derive):
- BTBlackboardValue<T> is a CLASS (reference type). ElephantBehaviorTree allocates one instance per blackboard value in its constructor. The builder reflection-COPIES that reference onto every node implementing the matching IBTBlackboard-derived interface, so every node shares the same instance: a SetValue by one node is visible to all readers. If the tree does NOT implement an interface that one of its nodes implements, or an interface property lacks get+set, the builder THROWS MissingTreeBlackBoardException at build (caught at attach; tree becomes null; logged).
- Selector.Prepare() evaluates each child's decorator once; a child whose BTReturnFalseDecorator returns false is skipped for that descent; the Selector runs executable children in order until one returns FinishedWithTrue. A decorator on a Selector/Sequence node is evaluated by that node's PARENT during the parent's descent.
- Tree root-eval delay base(N): BehaviorTreeAgentComponent.OnTick gates RunTree on (N/1000) in INTEGER math, so base(10) means the tree runs every component tick. Real pacing comes from SleepTask leaves (a SleepTask returns Running until its wall-clock duration elapses).

DOTS ID CHEATSHEET (for reference; this feature uses few IDs):
Monster id "dots_war_elephant"; culture aserai = Harad. Attack action codes act_elephant_attack_1..4 are registered in the EXTERNAL LOTRLOME_Armory module's action_types.xml and bound in its action_sets.xml (not in this repo) -- treat their existence as given; the code guards for non-resolution via ElephantAttackActions.AnyUnresolved().

FILES TO REVIEW:
Main/Features/Elephant/ElephantBehaviorTree.cs
Main/Features/Elephant/BehaviorTreeElements/IBTElephantBlackboard.cs
Main/Features/Elephant/BehaviorTreeElements/AttackOffCooldownDecorator.cs
Main/Features/Elephant/BehaviorTreeElements/EnemyInTrampleRangeDecorator.cs
Main/Features/Elephant/BehaviorTreeElements/ElephantAttackTaskBase.cs
Main/Features/Elephant/BehaviorTreeElements/ElephantAttackActions.cs
Main/Features/Elephant/ElephantAttackService.cs
Main/Features/Elephant/IElephantAttackService.cs
Main/Features/Elephant/ElephantConfig.cs
Main/Features/Elephant/ElephantMissionBehavior.cs
DOTS.Tests/Features/Elephant/ElephantAttackServiceTests.cs

KNOWN SUSPECTS (confirm or dispute each, with file:line evidence):
1. COOLDOWN ENGAGEMENT. The cooldown stamps (TrampleLastFired / SideAttackLastFired) are written by the attack tasks and read by AttackOffCooldownDecorator. Confirm the write is actually visible to the read (reference-shared BTBlackboardValue), so cooldowns genuinely rate-limit. A copy-by-value bug here would make every attack always-ready. Verify against the BT library, not by assumption.
2. BEARING WRITE-BEFORE-READ. TargetBearing is written by EnemyInTrampleRangeDecorator and read by ElephantSideAttackTask. Confirm the decorator runs (and writes) earlier in the same tree descent than the side-attack task executes -- otherwise the side attack reads a stale or zero bearing. Trace the actual Selector/Sequence execution order.
3. BEARING HANDEDNESS. EnemyInTrampleRangeDecorator computes bestBearing = lookDir.x*toEnemy.y - lookDir.y*toEnemy.x and ElephantSideAttackTask maps bearing >= 0 to the LEFT swing. Confirm positive cross-z = LEFT in Bannerlord's coordinate system (compare against TaleWorlds Vec2.LeftVec or equivalent), and that the >= 0 tie goes to a sane side.
4. ATTACK CADENCE. The "already mid-attack" gate (EnemyInTrampleRangeDecorator) blocks engagement while any attack clip plays. Trace the real timeline: trample at t=0 (~1.3s clip), side swings while it recharges, trample off cooldown at t=10. Is the trample-priority-then-side-fallback ordering honored at every evaluation instant? Is there any path to double-firing, a stuck state, or the trample being starved indefinitely by side swings?
5. ALREADY-ATTACKING DETECTION. The gate uses ElephantAttackActions.IsElephantAttack(GetCurrentAction(0)) (Index comparison against the 4 attack caches) instead of a string Contains. Confirm this covers EVERY clip the tasks actually play (including attack_4 / TrampleAlt) and cannot false-positive on locomotion/idle actions.
6. ATTACH COVERAGE. ElephantMissionBehavior attaches a BehaviorTreeAgentComponent to each elephant via a first-tick AllAgents scan plus OnAgentBuild late-spawn (gated on _treesAdded). Confirm no elephant can fall through both paths (never get a tree), and that BTRegister.RegisterClass("ElephantTree") in Initialize always runs before any attach attempt.
7. WALL-CLOCK COOLDOWNS. Cooldowns/sleeps use DateTime.Now (real time). This means they elapse during game pause and run real-time under slow-motion. The warg/library precedent does the same. Confirm this is the actual behavior and judge whether it is acceptable or a defect for this feature.
8. SIDE-ATTACK DAMAGE FOOTPRINT. ElephantSideAttackTask inherits the radial damage from ElephantAttackTaskBase -- it damages all enemies within TrampleRadius, not just the swung side. Confirm, and judge whether "a left swing hits right-side and rear enemies" is an acceptable phase-1 simplification or a correctness problem given the spec (animations + cooldowns).

REQUIRED ANALYSIS SECTIONS:
- Cooldown timeline correctness (suspect 4) -- walk an explicit t=0..12s trace.
- Cross-node state sharing (suspects 1, 2) -- grounded in the BT library.
- Engine API correctness -- verify the TaleWorlds calls against your knowledge of 1.4.5 (Agent.GetCurrentAction/GetCurrentActionType/SetActionChannel(in ActionIndexCache)/LookDirection/IsEnemyOf/ActionSet.IsValid, Mission.GetNearbyAgents, ActionIndexCache == operator + act_none, CustomAttacksUtils.TakeDamage). Flag anything that would not compile or would behave differently than the code assumes.
- Lifecycle / cross-elephant isolation -- per-elephant tree instances, no leaked static mutable state, clean teardown.
- ANYTHING ELSE -- bugs not covered by the suspects.

QUALITY GATES:
- Every finding needs file:line and a one-paragraph justification grounded in code you actually read.
- Cite the BT library or warg precedent when a pattern looks wrong but matches the proven reference.
- Severity: HIGH (crash / feature-dead / wrong-in-normal-play), MED (wrong in an edge case), LOW (style / micro-perf / accepted-behavior note).
- If a section has no real finding, say so explicitly.

PRIOR REVIEW LESSONS:
SUCCESSES: independent verification of state-sharing and lifecycle has caught real bugs; tracing the full execution order caught ordering gaps.
FAILURES TO AVOID: do not assume kingdom/culture IDs (empire = Dunland, NOT Rohan; vlandia = Rohan). Do not flag code that matches the warg precedent or the inlined BT library semantics as a bug. Do not skip the hard control-flow trace. Do not invent findings to fill sections.
