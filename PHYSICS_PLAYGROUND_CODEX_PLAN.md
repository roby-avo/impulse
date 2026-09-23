# Physics Playground: Codex Master Development Plan

## Mission

Build a polished, immediately playable 3D physics game in **Unity 6**
where a human competes directly against an AI opponent.

The AI opponent's **high-level decisions must be made exclusively by the
locally hosted Laya System-1 model**:

-   Model: `convaiinnovations/laya`
-   Model page: `https://huggingface.co/convaiinnovations/laya`
-   Inference must run locally.
-   Laya is the tactical decision maker.
-   Do not implement a hidden rule-based, random, behavior-tree,
    NavMesh, or other ML fallback policy.

The game is **Physics Playground**:

> Human and AI enter the same compact 3D physics arena. They move, jump,
> dodge, grab and throw objects, push the opponent, exploit the
> environment, and try to knock each other out of the arena.

The primary win condition is **ring-out**, not conventional HP
depletion.

## Core Principles

1.  **Fun first.** Responsive controls, satisfying impacts, readable
    physics, quick rematches.
2.  **Symmetric competition.** Human and AI operate under the same
    physical rules.
3.  **Laya decides, Unity executes.** Laya chooses semantic actions.
    Unity handles locomotion, animation, targeting, navigation,
    collision, grabbing, throwing and Rigidbody physics.
4.  **No fallback decision policy.** If Laya fails or is late, retry or
    wait. Never let another policy choose a tactical action.
5.  **Measurable.** Log AI decisions, latency, state and outcomes.
6.  **Modular.** Separate perception, decision, execution, physics,
    scoring and telemetry.

## Technology Stack

### Game

-   Unity 6
-   C#
-   Unity Input System
-   Rigidbody / PhysX
-   Unity AI Navigation / NavMesh for execution only
-   Cinemachine
-   URP initially

### AI

-   Local Python environment
-   `laya`
-   Laya weights cached locally
-   Local inference service
-   Unity communicates through localhost
-   Prefer Laya's official serving interface when compatible with the
    installed version

### Art

-   Blender for custom models and asset modification
-   Ready-made legal assets where useful
-   Prototype with Unity primitives before investing heavily in art

### Engineering

-   Git
-   README with reproducible setup
-   JSONL telemetry, with optional CSV match summaries

## Instructions to Codex

Actively implement the project rather than only describing what to do.

1.  Inspect the existing Unity project before modifying it.
2.  Preserve working project configuration.
3.  Create/modify scripts, scenes, prefabs, materials, configuration and
    supporting Python files directly.
4.  Run available validation after meaningful changes.
5.  Fix compilation errors before moving forward.
6.  Keep the architecture simple and modular.
7.  Do not add unnecessary packages.
8.  Do not block gameplay work on Blender.
9.  Maintain `DEVELOPMENT_STATUS.md` with completed work, playable
    state, known issues and next milestone.
10. When unavoidable manual Unity Editor actions are required, minimize
    them and state the exact action.

## MVP Gameplay

### Arena

Create one compact arena suspended above a knockout volume.

Start with a **stylized rooftop / industrial physics arena**
containing: - open edges - room for maneuvering - cover - physics
props - clearly readable boundaries - opposite spawn points

Do not build multiple maps until this map is fun.

### Human Character

Third-person controller with: - move - look/rotate - jump - dodge -
grab/release - throw - push / short-range physical attack

Movement should be responsive rather than permanently ragdoll-driven.
Physics becomes prominent during impacts, knockback, falling, throws and
temporary loss of balance.

### Initial Props

**Crate:** light/medium, easy to grab, moderate knockback.

**Chair:** light, easy projectile, potentially useful as cover.

**Barrel:** heavier, rolls naturally, stronger knockback.

**Table/large box:** difficult to throw, pushable, provides cover.

Use reusable components such as `PhysicsInteractable`, `Grabbable`,
`Throwable` and `KnockbackProvider` rather than object-specific logic.

## Round Structure

A player loses when entering the knockout volume.

``` text
Countdown
  -> Fight
  -> Ring-out
  -> Winner
  -> Short delay
  -> Reset players and props
  -> Next round
```

Initial match format: **first to 5 round wins**, configurable.

Reset positions, velocities, props and transient arena state reliably.

## AI Architecture

``` text
Unity World
    |
    v
AI Perception
    |
    v
Compact State Snapshot
    |
    v
Laya Client
    |
    | localhost
    v
Local Python Laya Service
    |
    v
Laya Decision
    |
    v
Unity Action Executor
    |
    v
Movement / Physics
```

Suggested Unity components: - `AIPerception.cs` - `AIStateBuilder.cs` -
`LayaClient.cs` - `LayaDecisionController.cs` - `AIActionExecutor.cs` -
`AIDecisionLogger.cs`

Names may change if a cleaner design is justified.

## Laya vs Unity Responsibilities

Laya decides **what to do**.

Unity decides **how to execute that already-selected action
physically**.

Do not have Laya control: - Rigidbody forces every frame - animation
frames - NavMesh corners - joystick axes - camera rotation

Example:

``` text
Laya -> GRAB_NEAREST_OBJECT

Unity executor:
1. use the object identified in the decision context
2. turn/navigate toward it
3. enter interaction range
4. grab it
5. report success/failure
```

## Initial Action Space

Keep the first action space small:

``` text
APPROACH_OPPONENT
RETREAT_FROM_OPPONENT
PUSH_OPPONENT
GRAB_NEAREST_OBJECT
THROW_HELD_OBJECT_AT_OPPONENT
DODGE_LEFT
DODGE_RIGHT
MOVE_TOWARD_SAFETY
TAKE_COVER
JUMP
```

Later candidates: - GRAB_OPPONENT - THROW_OPPONENT - CIRCLE_LEFT -
CIRCLE_RIGHT - MOVE_TOWARD_OBJECT - USE_ENVIRONMENT

Do not introduce dozens of actions initially.

## State Given to Laya

Use a compact semantic state, not a raw Unity dump.

Example:

``` json
{
  "self": {
    "near_edge": false,
    "distance_to_edge_m": 4.2,
    "holding_object": false,
    "speed": 2.1,
    "balance": 0.91
  },
  "opponent": {
    "distance_m": 3.1,
    "near_edge": true,
    "facing_self": true,
    "holding_object": true,
    "held_object_type": "barrel"
  },
  "nearest_object": {
    "type": "chair",
    "distance_m": 1.3,
    "mass_class": "light"
  },
  "threats": {
    "incoming_projectile": false
  }
}
```

Candidate observations: - opponent distance/direction/velocity -
opponent near edge - own distance to edge - held objects - nearest
useful object - incoming projectile - cover availability - recent
opponent action - current action status - previous Laya action and
outcome

Do not give Laya privileged information unavailable to the human unless
an experiment explicitly requires it.

## Laya Request

Use Laya's supported structured System-1 interface, preferably a
**choice** question over allowed actions.

Conceptually:

> Given the current state, which action should the AI take now?

Before implementation, inspect the installed Laya package/model
documentation and implement against the actual current API. Do not guess
API signatures.

Preload the model so weights are not reloaded during gameplay.

## Local AI Service

Create:

``` text
ai/
  README.md
  requirements.txt or pyproject.toml
  start script(s)
  smoke_test.py
```

Requirements: - local-only inference - model loaded once -
readiness/health check - decision endpoint - timeout/error detection -
latency measurement - clear diagnostics

Smoke test: 1. model/service starts 2. valid state is submitted 3. one
allowed action is returned

## Strict No-Fallback Rule

Forbidden:

``` text
if Laya fails:
    choose_random_action()
```

Forbidden:

``` text
if timeout:
    attack()
```

Forbidden:

``` text
if confidence_low:
    behavior_tree.decide()
```

Required behavior:

``` text
request Laya
    |
 valid?
 /    \
yes    no
 |      |
execute record failure
        |
   finish current committed action
   if safe, otherwise neutral wait
        |
       retry
```

A retry is not a fallback decision policy.

Low-level navigation toward a target selected by Laya is execution logic
and is allowed. Choosing a new tactical target locally is not.

## Decision Scheduling

Do not query Laya every rendered frame.

Implement an asynchronous decision loop: - Unity rendering and physics
continue normally. - Request a decision when a new decision is needed. -
Never block Unity's main thread waiting for inference. - Execute the
returned decision. - Reassess on completion, interruption or
configurable interval. - Measure actual request/response latency. - Do
not artificially hide Laya latency.

## Interruptions

The world may change while an action is executing.

Request a fresh Laya decision after important events such as: - incoming
dangerous projectile - sudden near-edge state - target object
disappears - current action becomes impossible - major
collision/knockdown - opponent becomes immediately pushable

Do not choose the replacement action locally.

## Fairness

Where practical, human and AI use: - same movement speed - same jump -
same knockback - same grab range - same throw physics - same collision
rules - same arena

Inference/reaction latency is part of the experiment and should be
measured rather than silently equalized.

## Telemetry

For each Laya decision record:

``` text
match_id
round_id
decision_id
game_time
state_snapshot
available_actions
selected_action
confidence/probability if available
request_timestamp
response_timestamp
latency_ms
action_start
action_end
execution_success
interruption_reason
resulting_state
```

Log comparable human events: - jump - grab - throw - push - dodge -
ring-out

Do not claim to measure human reaction time without defining a proper
stimulus-response experiment.

## Match Statistics

Show a concise comparison: - round wins - ring-outs - self ring-outs -
objects grabbed/thrown - throw hit rate - successful pushes -
dodges/evasions - time near edge - average distance from edge -
environmental knockouts - average AI decision latency - Laya action
distribution

Later research metrics can include calibration, opportunity
exploitation, risk preference, predictability and human exploitation of
AI behavior.

## Visual Direction

Target **stylized polished 3D**, not photorealistic AAA.

Priorities: - readable silhouettes - exaggerated but coherent props -
attractive lighting - clear arena boundaries - strong impact feedback -
restrained UI - fast restart - visually distinct human and AI characters

## Camera and Feedback

Use a responsive third-person camera.

Add progressively: - impact sounds - footsteps - grab/release audio -
particles for strong collisions - subtle camera impulse - hit feedback -
ring-out effect - countdown and victory feedback

Avoid excessive camera shake.

## Blender Strategy

Blender is part of the production pipeline but not a gameplay blocker.

**Phase 1:** Unity primitives/simple assets.

**Phase 2:** Blender for arena geometry, railings, platforms, props and
environment pieces.

**Phase 3:** custom characters/robots, rig changes, animations,
destruction variants and stronger visual identity.

Maintain consistent scale between Blender and Unity and prefer modular
assets.

# Development Milestones

## M0: Foundation

Deliver: - project opens without errors - Git configured - URP/Input
System configured - clean folders - test scene - README

Suggested layout:

``` text
Assets/
  Art/
  Audio/
  Materials/
  Prefabs/
  Scenes/
  Scripts/
    AI/
    Characters/
    Combat/
    Interaction/
    Physics/
    Game/
    Telemetry/
    UI/
  Settings/
```

## M1: Human Physics Sandbox

Deliver: - arena - human controller - camera - jump/dodge - props -
grab/throw - push - knockout volume - reset

Acceptance criterion:

> Human can run, grab a barrel, throw it, push objects, fall out and
> restart reliably.

## M2: AI Physical Avatar

Deliver: - AI character with equivalent physical parameters - reusable
action executor - developer debug panel to manually trigger semantic
actions

The debug panel tests execution only. It is not a fallback AI.

Acceptance criterion:

> Every exposed semantic action can be manually triggered and completes
> or reports failure reliably.

## M3: Real Local Laya Integration

Deliver: - local Laya environment - model preload - smoke test - async
Unity client - state serialization - structured choice request -
response validation - latency logging

Acceptance criterion:

> Unity sends a real game state to locally running Laya and receives a
> valid semantic action during Play Mode.

## M4: Human vs Laya

Deliver: - autonomous decision loop - action interruptions - full
rounds - first-to-5 match - no fallback tactical policy - telemetry

Acceptance criterion:

> Human can play a complete match against an opponent whose tactical
> actions are selected by Laya.

## M5: Game Feel

Improve: - physics tuning - movement - animation - camera - audio -
VFX - UI - arena readability - prop placement

Acceptance criterion:

> The game is enjoyable enough that repeated matches are desirable even
> without looking at research metrics.

## M6: Visual Upgrade

Use Blender and/or appropriate assets to replace prototype geometry
while preserving mechanics.

Do not destabilize the working game for visual polish.

## M7: Experimental Platform

Add: - richer telemetry - match replay/event inspection if practical -
configurable Laya decision frequency - configurable action/state
schemas - experiment IDs - exportable results

# First Implementation Order

Begin in this exact order unless the existing project makes another
order clearly superior:

1.  Inspect Unity installation/project.
2.  Establish repository/folders/documentation.
3.  Create arena.
4.  Create human controller and camera.
5.  Add knockout/reset.
6.  Add one grabbable cube.
7.  Implement grab and throw.
8.  Add push.
9.  Tune basic physics until it is fun.
10. Add AI avatar.
11. Implement action executor.
12. Test every AI action manually.
13. Set up local Laya environment.
14. Verify Laya independently with smoke test.
15. Connect Unity to Laya asynchronously.
16. Build semantic perception/state.
17. Enable autonomous Laya decision loop.
18. Add complete match loop.
19. Add telemetry.
20. Polish gameplay.
21. Upgrade art with Blender.

# Non-Goals for the First Version

Do **not** initially build: - multiplayer networking - campaign/story -
inventory system - skill trees - procedural maps - many arenas - many
characters - complex destruction - cloud inference - reinforcement
learning - LLM fallback - sophisticated menus - photorealistic graphics

# Definition of Success

The first major success state is:

> I launch the game, immediately control my character in a polished 3D
> physics arena, and face an AI opponent controlled tactically by
> locally running Laya. We both obey the same physical rules. We grab
> and throw objects, dodge, push each other and exploit the environment.
> One of us falls out of the arena. The round resets quickly. After
> several rounds I can see who performed better and inspect Laya's
> decisions and latency.

At that point, the project is both a **fun game prototype** and a
**System-1 decision-making testbed**.

# Final Instruction to Codex

Do not merely produce a large plan in response to this file. Treat this
document as the implementation specification.

Start by inspecting the available project/environment, state briefly
what exists, then implement **Milestone 0 and Milestone 1**. Validate
the result before proceeding. Continue autonomously through later
milestones when the environment permits, stopping only for genuinely
blocking information or an action that cannot be performed with the
available tools.
