# GIRLS & MINING

## Game Design Document — Small-Scope Production Version v1.0

**Genre:** Mining-defense roguelite / incremental action strategy
**Platform:** PC / Steam
**Target price:** **$3.99 USD**
**Target first clear:** 2.5–4 hours
**Target completion / optimization:** 6–10 hours
**Camera:** Fixed GTA 1-style top-down / high orthographic
**Visual production:** Blender-authored assets rendered to 2D sprites with dynamic effects in-engine
**Structure:** 6 replayable sectors + permanent research progression
**Core fantasy:** Build an increasingly profitable mining operation on a hostile alien world and hold it together as thousands of creatures descend on it.

---

# 1. High Concept

## Elevator Pitch

**GIRLS & MINING is a top-down mining-defense game where you control an industrial robot, establish automated mining operations, fortify them with turrets and support structures, and extract as much ore as possible while increasingly massive alien swarms attack the operation.**

Mining faster makes you richer.

Expansion gives access to richer deposits.

But every expansion increases the area you must defend.

Every run produces permanent research that makes the next operation stronger.

Eventually the player reaches the point where a few mining machines surrounded by insects has transformed into an industrial fortress firing into **thousands of enemies simultaneously**.

---

# 2. Design Goal

The overriding production objective is:

> **Build the smallest viable game capable of producing visually spectacular battles.**

GIRLS & MINING is deliberately not trying to compete through enormous amounts of content.

Its value comes from:

* enormous enemy counts;
* strong visual escalation;
* a satisfying mining/defense feedback loop;
* meaningful optimization;
* permanent incremental progression;
* a highly distinctive presentation;
* very high audiovisual polish relative to its price.

Every system must strengthen at least one of those goals.

---

# 3. Core Design Pillars

## Mine

The player needs resources to become stronger.

Ore deposits are finite and geographically separated, so staying in one safe position forever is impossible.

---

## Expand

The richest deposits are farther from the starting base.

The player must establish new drills and extend the operation outward.

Expansion improves income but stretches the defensive perimeter.

---

## Defend

Mining activity attracts increasingly large alien populations.

The player constructs automated defenses while personally intervening with the robot.

---

## Survive

The enemy pressure eventually becomes extreme.

A successful player survives the authored sector objective and can then extract safely or continue beyond the normal mission for greater scores.

---

## Improve

Even failed expeditions generate permanent research progress.

What nearly destroyed the player during an early attempt should eventually become trivial.

That is the incremental fantasy.

---

# 4. Theme and World

## Setting

Humanity has entered an era of industrial extraterrestrial extraction.

**K-01 Industries** sends semi-autonomous mining operations to worlds that are resource-rich, inhospitable and biologically hostile.

The player operates an industrial mining chassis identified simply as:

**Miner-01**

The corporation treats each hostile world as an economic opportunity.

Its messaging is relentlessly optimistic:

> **RESOURCES TODAY. A BRIGHTER TOMORROW.**

The game does not require extensive lore or dialogue.

The world is communicated through:

corporate terminology, equipment markings, mission briefings, sector reports, research descriptions and environmental storytelling.

The tone should feel like an **1980s industrial science-fiction film viewed through a mining corporation's terminal system**.

Not cyberpunk.

Not colorful space fantasy.

More:

**Alien + Aliens + old military terminals + industrial mining hardware + hostile planetary exploitation.**

There is a subtle satirical edge, but the game should not constantly tell jokes.

---

# 5. Visual Theme

The world and interface use deliberately different visual languages, with illustrated pilot portraits providing the human face of the machinery.

## The World

Gameplay itself is visually rich.

The environment uses:

* dark rock;
* ash;
* worn industrial metal;
* pale mineral deposits;
* desaturated machinery;
* subdued terrain colors.

Color comes primarily from **effects**:

* muzzle flashes;
* explosions;
* mining lasers;
* glowing ore;
* acid;
* sparks;
* overheating machinery;
* warning lamps;
* enemy death effects.

The base geometry and sprites should remain slightly simplified.

The effects make the game look expensive.

This lets the Blender asset workflow remain fast while combat still becomes visually spectacular.

## Mech Pilot Portraits

Mech portraits depict **adult anime-style female pilots**, not just pictures of the chassis.

The art direction combines expressive, recognizable faces with industrial flight suits, workwear, headsets and mining-corporation equipment. Use restrained colors and lighting that sit comfortably beside the dark world and monochrome terminal UI; portraits are character artwork, not a replacement for the UI's semantic color palette.

Design portraits for readable head-and-shoulders crops at small HUD sizes. Keep framing, lighting and illustration style consistent across the portrait set. Supply clean, high-resolution source artwork with a transparent background so the shared UI can handle framing and scaling.

Use the pilot portrait in the existing mech-status HUD and mission presentation where appropriate. Do not introduce a separate crew screen.

For 1.0, pilot identity is **presentation only**. The game still has one playable Miner-01 chassis and one shared gameplay ruleset. Portraits do not imply character classes, pilot stats, crew management or dialogue trees. Start with one production-ready portrait; the final portrait count remains an art-budget decision.

---

# 6. UI Theme

The interface is a **monochrome industrial terminal**, not a green-phosphor interface.

Almost everything is:

**black → charcoal → gray → off-white.**

Color is exceptional.

## Orange

Orange indicates:

* current selection;
* active tab;
* focus/highlight;
* player attention;
* current upgrade;
* important interactive action;
* imminent warning.

Recommended primary orange:

**#E9943A**

Bright bloom/highlight:

**#FFA143**

---

## Green

Green has exactly one semantic family:

> **successful / completed / purchased / valid / operational-confirmed state**

Examples:

* completed objective;
* researched upgrade;
* successfully cleared sector;
* valid placement;
* successful save;
* machine online confirmation.

Green is **not general UI decoration**.

Recommended:

**#79C98B**

---

## Neutral UI palette

| Role             | Value     |
| ---------------- | --------- |
| Deep black       | `#050505` |
| Background       | `#090A09` |
| Raised surface   | `#111211` |
| Control surface  | `#181918` |
| Border           | `#393B39` |
| Disabled         | `#70736F` |
| Secondary text   | `#B7BAB6` |
| Primary text     | `#E8EAE7` |
| Selected orange  | `#E9943A` |
| Orange highlight | `#FFA143` |
| Success green    | `#79C98B` |

Enemy/threat information should generally remain **white/gray plus hostile iconography**, becoming orange when immediate player attention is required.

No blue UI resources.

No rainbow research branches.

The interface should remain almost entirely monochromatic.

---

# 7. CRT Presentation

The entire rendered game is displayed as if through a rugged industrial CRT terminal. CRT is applied once as the final backbuffer-sized post-process after the scene and UI have both rendered, never as a material owned by a UI element. It remains active in menus and gameplay.

Use:

* subtle horizontal scanlines;
* faint two-dimensional screen-space grain without directional banding;
* mild radial vignette;
* slight bloom;
* occasional terminal response animation.

A single progress-slider scales the original Aged CRT treatment from off to full strength. Zero means disabled and any non-zero intensity means enabled. Scanlines span the full screen horizontally; no vertical or diagonal scanlines are used. Screen curvature is not used.

Avoid:

* aggressive distortion;
* constant flicker;
* blurry text;
* excessive chromatic aberration.

White text should remain sharp.

Bloom is strongest on **orange selection** and more restrained on green success indicators.

The underlying UI contract already requires concise functional copy, a 1280×720 baseline, PC-first mouse/keyboard interaction, accessibility accommodation and no core interaction communicated only through color.

---

# 8. Camera and Rendering

## Camera

Fixed high orthographic camera.

The perspective should evoke **GTA 1**, rather than a modern angled isometric game.

This is a hard production limitation.

No player camera rotation.

No cinematic 3D camera.

Limited zoom may be considered, but the authored gameplay perspective remains fixed.

---

## Asset workflow

Gameplay assets are:

1. modeled in Blender;
2. lit from the final camera;
3. rendered to sprite sheets;
4. optionally exported with supporting masks/normals;
5. combined with runtime particles, lighting and bloom.

Mechanical structures benefit especially from this pipeline because they need relatively little skeletal animation.

---

# 9. Core Run Loop

The core loop is:

**Mine ore**

→ spend ore on stronger production and defenses

→ mining increases hostile pressure

→ defend against larger swarms

→ current deposits begin to become insufficient

→ expand toward richer deposits

→ defensive perimeter becomes harder to maintain

→ mine even faster

→ survive increasingly large attacks

→ complete sector objective

→ extract or continue for score

→ receive Research Cores

→ buy permanent upgrades

→ return stronger.

---

# 10. Player Robot

The player directly controls **Miner-01**.

Miner-01 is the industrial mech; its human pilot is represented through the portrait direction in Section 5. Pilot presentation does not change the chassis or its abilities.

The robot gives the player something active to do while the base operates automatically.

## Core actions

### Movement

Direct WASD/gamepad movement.

### Primary weapon

Simple ranged industrial weapon.

No enormous weapon inventory.

### Mining

The robot can manually mine designated resource deposits early in a run.

### Building

Approach an available hardpoint and choose a structure.

### Repairing

The robot can repair damaged structures directly.

---

# 11. Robot progression during a run

Run upgrades can improve:

* weapon output;
* mining effectiveness;
* movement;
* repair rate;
* survivability;
* utility.

The robot should begin useful but not capable of replacing an entire defensive network.

Its role is:

> **rapid response and intervention.**

Turrets hold territory.

The robot fixes whatever is going wrong.

---

# 12. Building System

## No free placement

Construction uses **authored hardpoints**.

This is a strict scope decision.

It avoids:

* dynamic pathfinding reconstruction;
* tower-overlap exploits;
* wall mazes;
* placement edge cases;
* ugly bases;
* excessive balancing work.

The player still decides what each usable hardpoint becomes.

---

# 13. Hardpoint Types

## Mining Site

Attached directly to an ore deposit.

Can build:

**Mining Drill**

---

## Defense Hardpoint

Can build:

**Gun Turret**

or

**Blast Turret**

---

## Support Hardpoint

Can build:

**Repair Station**

---

## Barricade Slot

Predetermined defensive position.

Can build:

**Barricade**

This produces a maximum of **five buildable structures**.

---

# 14. Buildings

## Mining Drill

Automatically extracts ore.

The primary economic structure.

Increasing extraction rate also accelerates hostile escalation.

---

## Gun Turret

Fast reliable single-target fire.

Excellent against standard swarm enemies.

---

## Blast Turret

Slow area damage.

Essential against dense groups.

---

## Repair Station

Automatically repairs nearby infrastructure and can support the player robot.

---

## Barricade

Cheap defensive structure that slows or redirects enemy flow through predefined routes.

No arbitrary wall construction.

---

# 15. Ore

There is only **one normal in-run currency**.

# ORE

Ore is:

* harvested from deposits;
* automatically added to the operation;
* spent on structures;
* spent on structure upgrades;
* spent on the in-run technology tree.

This deliberately creates competition between:

> **building right now**

and

> **investing in long-term run efficiency.**

Do not add wood, metal, energy crystals, credits and five additional materials.

---

# 16. Mining and Expansion

Every sector contains multiple ore zones.

The player begins near a modest deposit.

More lucrative deposits are positioned farther outward.

The starting deposit should not comfortably support the entire mission.

Therefore the player eventually faces:

> expand or become economically stagnant.

Opening a new mining site:

* increases ore income;
* activates additional build hardpoints;
* increases the territory under attack;
* can activate additional enemy approaches;
* raises the practical difficulty of defense.

Expansion is therefore always attractive and dangerous.

---

# 17. Threat

Mining generates **Threat**.

Threat represents how badly the native ecosystem has been disturbed.

Threat rises based primarily on:

* total ore extracted;
* number of active drills;
* sector progression.

Higher Threat increases:

* enemy density;
* spawn frequency;
* elite frequency;
* enemy composition.

This means player greed partially determines difficulty.

A conservative player can stabilize.

A min-max player can aggressively overclock extraction and attempt to survive the consequences.

---

# 18. Waves

The campaign uses a **hybrid wave structure**.

Each sector contains:

**12 authored waves.**

Waves provide:

* readable pacing;
* score comparison;
* downtime for construction;
* clear objectives;
* predictable balancing;
* a strong progression benchmark.

Between waves the game provides a short construction interval.

Threat still modifies the intensity of each wave.

Therefore two players at Wave 8 can have substantially different enemy pressure depending on how aggressively they mined.

---

# 19. Endless Extension

After Wave 12 the sector is considered cleared.

The player receives the choice:

# EXTRACT

Secure the current run and complete the mission.

or:

# CONTINUE DEEPER

Enter Endless pressure.

Enemy counts, health and composition continue scaling.

The player attempts to extract more Ore and improve their sector record.

This provides replayability for min-maxers with almost no additional authored content.

---

# 20. Sector Records

Each sector stores:

* best Ore extracted;
* highest wave;
* longest Endless survival;
* best clear time;
* optional score.

The most important visible record is:

# BEST ORE

This directly reinforces the mining fantasy.

---

# 21. Enemy Design

The game is built around **quantity rather than a huge bestiary**.

Target:

**5 enemy archetypes + 1 boss family.**

Several can share rigs/body components.

---

## Swarmer

Small.

Fast.

Low health.

Can appear in enormous numbers.

The primary visual spectacle enemy.

---

## Spitter

Stops at range and fires acid.

Forces the player to deal with threats outside the immediate defensive perimeter.

---

## Brute

Large armored creature.

Slow and durable.

Forces higher single-target damage.

---

## Burrower

Appears through designated internal breach points.

Prevents complete reliance on the outer perimeter.

Use sparingly.

---

## Brood Carrier

Releases multiple smaller enemies when killed.

Creates sudden density spikes.

---

## Matriarch

Large boss/elite class.

Used for major wave climaxes and the final sector.

---

# 22. Enemy Count

The technology should be optimized for swarms.

Design target:

**hundreds of fully active enemies routinely**

with peaks in the **low thousands** during late/endless play.

The enemy simulation should prefer:

* simple movement;
* group steering;
* flow fields;
* reduced update rates at distance;
* lightweight collision;
* object pooling.

No complicated per-creature decision trees.

The player's perception should be:

> **a living wall is coming toward the operation.**

---

# 23. Combat Spectacle

Graphics and effects carry a disproportionate amount of production value.

Late game should contain:

* dense tracer fire;
* hundreds of enemies;
* explosions;
* glowing impacts;
* acid sprays;
* sparks;
* destroyed enemies accumulating temporarily;
* smoke;
* mining machinery continuing to work during combat;
* warning lights;
* damaged machinery;
* screen-filling attack pressure.

The base visual assets can remain relatively simple because the final image is composed from **quantity + effects + lighting**.

---

# 24. In-Run Upgrade Tree

The run does **not** use random card drafting.

Instead the player has access to a large terminal-style **Field Technology Tree**.

The tree should look enormous but remain cheap to implement.

Target:

**approximately 32 meaningful nodes.**

Four branches:

### Mining

Extraction speed, deposit efficiency, scanning, automated handling.

### Defense

Turret damage, AoE, range, barricades, repairs.

### Robot

Weapon, mining tool, movement, armor, repair capability.

### Systems

Economy, construction, global support, Threat manipulation.

Nodes can have multiple ranks where appropriate.

---

# 25. Run Upgrade Philosophy

Most nodes should fall into three groups.

### Basic numerical improvements

Cheap connective progression.

Example:

**Turret Calibration**
+15% turret damage.

### Capability upgrades

Example:

**Piercing Rounds**
Gun Turrets can hit an additional target.

### System interactions

Example:

**Salvage Protocol**
Elite enemies have a chance to generate Ore.

The tree should gradually turn the player's operation from:

> mining equipment with guns

into:

> an absurd industrial war machine.

All in-run upgrades reset after the mission.

---

# 26. Permanent Incremental Progression

There is **no additional prestige reset system**.

The permanent progression system is already the incremental layer.

Currency:

# RESEARCH CORES

Research Cores are awarded after expeditions based on:

* mission progress;
* extracted Ore;
* new records;
* sector completion;
* optional objectives.

Even failed runs should award some progress.

---

# 27. Permanent Research Tree

Permanent research is accessed from the Command Center.

Target:

**approximately 24 permanent nodes**.

Four sectors:

### Extraction

Better starting mining capability.

### Defense

Stronger initial base/turrets.

### Robot

Permanent chassis improvements.

### Infrastructure

Better economy, repair and construction.

The UI presents them as large monochrome terminal boxes connected into readable branches.

---

# 28. Permanent Research Philosophy

The player should become noticeably stronger between attempts.

Examples:

**Industrial Calibration I–III**
Mining Drill output increase.

**Fortified Core I–III**
Core durability.

**Veteran Turrets**
Basic defenses begin with an improvement.

**Improved Chassis**
Robot durability.

**Emergency Stores**
Begin each mission with additional Ore.

**Field Diagnostics**
Reduced repair costs.

The game should not require hundreds of +1% nodes.

---

# 29. Failure

The central mining operation contains the:

# OUTPOST CORE

If the Core reaches zero health:

**the expedition is lost.**

The game does not erase progression.

The Results screen calculates Research Cores and records the attempt.

The player returns to the Command Center and may immediately purchase improvements.

---

# 30. Robot Destruction

Robot death does **not** immediately end the mission.

Miner-01 becomes disabled and is reconstructed by the Outpost after a short delay.

During this period:

* the player cannot intervene;
* defenses operate automatically;
* repair capacity is reduced.

This makes robot death meaningful without abruptly ending a strong run.

The Core remains the true fail condition.

---

# 31. Mission Completion

Surviving Wave 12 unlocks extraction.

Choosing Extract:

* ends the run;
* marks the sector cleared;
* secures the run's rewards;
* unlocks progression when applicable.

Continuing Endless does not risk previously earned sector-clear status, but the additional Ore/research reward is only secured when the Endless attempt finally ends.

---

# 32. Campaign Structure

There are **6 sectors**.

They reuse a shared modular industrial/planetary art library.

They should not require six completely independent environment production pipelines.

Each sector changes primarily through:

* terrain composition;
* layout;
* ore deposit distribution;
* hardpoint arrangement;
* enemy directions;
* environmental tint/lighting;
* one gameplay modifier.

Suggested campaign:

| Sector                 | Identity                      | Main twist                     |
| ---------------------- | ----------------------------- | ------------------------------ |
| **A-3 Crystal Basin**  | Introductory mining basin     | Basic systems                  |
| **D-7 Ashen Ridge**    | Long exposed extraction lanes | More attack directions         |
| **K-1 Frozen Expanse** | Sparse rich deposits          | Greater travel distance        |
| **M-4 Magma Fields**   | Dangerous industrial terrain  | Temporary hardpoint disruption |
| **R-9 Shattered Moon** | Low-cover open terrain        | High swarm density             |
| **Z-2 The Hollow**     | Deep hostile nest             | Final Matriarch pressure       |

---

# 33. Target Mission Length

A successful first campaign clear of a sector:

**12–18 minutes.**

Experienced optimized clear:

**8–12 minutes.**

Endless:

player-determined.

Six sectors therefore do not imply a six-hour linear campaign.

Replay and permanent progression produce the rest of the playtime.

---

# 34. Results Screen

After every expedition:

# OPERATION REPORT

Displays:

* sector;
* result;
* wave reached;
* Ore extracted;
* enemies destroyed;
* structures lost;
* best-record indicators;
* Research Cores earned.

Actions:

**RETURN TO COMMAND**

**RETRY**

**NEXT SECTOR** when appropriate.

Failure should lead into progression in seconds.

---

# 35. Command Center

The between-run hub has exactly two tabs.

## Mission

Contains:

* six-sector list;
* selected-sector mission briefing;
* best records;
* threat information;
* known enemy portraits;
* modifiers;
* mission start.

## Research

Contains:

* full permanent research tree;
* selected-node details;
* Research Core balance.

No additional hub screens are required.

---

# 36. Required UI Screens

The complete production UI is:

1. **Main Menu**
2. **Settings**
3. **Save / Load**
4. **Command Center — Mission tab**
5. **Command Center — Research tab**
6. **Gameplay HUD**
7. **In-Run Upgrade Tree**
8. **Results / Operation Report**

Plus lightweight:

* pause overlay;
* confirmation dialog;
* tooltip;
* autosave message.

That is the complete UI scope.

The uploaded UI contract also requires explicit hover, focus, pressed, disabled, selected, error and busy states, with keyboard/controller focus distinguishable from mouse hover.

---

# 37. Save System

Three campaign save slots.

Each save stores:

* sector progress;
* permanent research;
* personal records;
* settings/profile metadata.

Missions do not need arbitrary manual mid-run saves unless implementation makes them effectively free.

Autosave:

* before mission;
* after Results;
* after research purchase.

---

# 38. Controls

## Robot

**WASD** — movement
**Mouse** — aiming
**LMB** — primary tool/weapon
**E** — interact/build
**R** — repair where appropriate
**Tab** — in-run technology tree
**Esc** — pause

Exact mapping remains remappable once the required system exists.

The UI design requirements establish mouse/keyboard as first priority and controller second.

---

# 39. Accessibility

Required design considerations:

* UI scale;
* text-size option;
* reduced screen shake;
* reduced bloom/flashing;
* damage numbers toggle;
* color-independent status icons;
* pause at any time;
* readable contrast;
* controller focus indicators.

Green success and orange selection always have accompanying:

* icon;
* border treatment;
* text/state label.

Never color alone.

---

# 40. Audio Direction

The soundscape should feel industrial and oppressive.

## Machinery, Combat and Interface

Important sounds:

* drill machinery;
* ore cracking;
* turret fire;
* metal impacts;
* repair tools;
* enemy swarm movement;
* distant creature calls;
* industrial alarms;
* terminal clicks;
* CRT/relay sounds.

Terminal UI interactions should sound tactile:

relay click, key chirp, confirmation tone, warning pulse.

## Music Identity

The soundtrack is **heavy rock / industrial metal with a deliberate ramp-up**: a restrained mechanical pulse during preparation, driving riffs as the swarm develops, and a full, hard-hitting arrangement at peak pressure. Orchestral rock can give major encounters an additional sense of scale.

Intensity should come from the arrangement and rhythm, not simply turning up the volume. Quiet stretches make the heavy sections land. Prefer instrumental tracks and preserve the audibility of weapons, incoming attacks and critical alarms.

## Ovani Sound Audition Shortlist

These are catalog-based candidates, not final track selections or listening-verified recommendations. Audition the versions available in the team's licensed library before locking individual cues. Research checked on 2026-09-16.

| Candidate | Proposed role |
| --- | --- |
| [Metal Music Pack Vol. 1](https://ovanisound.com/products/metal-music-pack-vol-1) | First audition for the core combat sound: aggressive, high-energy metal. |
| [Industrial Music Pack Vol. 1](https://ovanisound.com/products/industrial-music-pack-vol-1) | First audition for mechanical tension, preparation and the transition into combat. |
| [Orchestral Rock Music Pack Vol. 1](https://ovanisound.com/products/orchestral-rock-music-pack-vol-1) | First audition for Matriarch encounters and major wave climaxes; guitars and drums with orchestral weight. |

Each linked pack advertises ten compositions with three intensity versions per composition, plus short edits. Those versions are promising material for the ramp-up, but their timing and loop compatibility must be checked in the actual files. The shortlist does not require purchasing or using all three packs.

Choose a small, coherent set of cues with satisfying low, medium and high arrangements. Test them against real combat SFX and repeated loops, not just a standalone preview. Record the source pack, track name and license provenance for every selected asset.

## Adaptive Music System — Engine Foundation Implemented

Build an original, reusable music system for the MonoGame runtime. Ovani's [Music Plugin](https://ovanisound.com/products/unity-audio-plugin) is the functional reference for intensity changes, song selection and seamless looping; beat/bar scheduling and the enemy rules below are our own requirements, not assumptions about that plugin's internals.

The engine now supplies Master, Music, FX, Ambience and UI volume controls, 2D positional effects, gain-based ambience zones, and sample-synchronized intensity/cue playback. See the [engine audio implementation notes](Engine/Audio/README.md). A provisional two-cue selection from the supplied Metal Vol. 3 pack is wired: **Fallen Angel** for the menu, **Chin Surgery** for session preparation, plus hover/click sounds from UI & Menus. All three intensity mixes are preloaded; transitions currently use timed crossfades. Embedded tempo labels and supplied reverb-tail values are recorded in the [asset notes](Content/Audio/README.md). Final audition, downbeat/meter verification and the game-side enemy/threat director remain **planned work**. This foundation does not include environmental reverb/occlusion DSP or disk-streamed music.

Separate two responsibilities:

* **Engine audio playback:** shared audio timeline, looping, synchronized playback, scheduled crossfades and channel gains. This lives independently of scenes and UI.
* **Game music director:** chooses the cue and target intensity from operation phase, sustained enemy pressure and encounter type. Scenes report gameplay state; they do not implement their own music players or transition timing.

Support two distinct kinds of change:

1. **Intensity ramp within a cue:** move between its low, medium and high versions while preserving the musical position. A rise in pressure should build the arrangement without restarting the song. Smooth the requested intensity and use separate rise/fall thresholds so it does not chatter between versions.
2. **Transition between cues:** change musical identity for a different encounter. Schedule the change at an authored bar or phrase boundary with a suitable crossfade, bridge or short transition accent. Boss arrival may trigger an immediate warning accent while the music waits for its transition point.

Treat Ovani intensity versions as alternate mixes unless the supplied assets explicitly contain additive stems. Do not simply stack three full mixes at full gain. Verify matching tempo, downbeat alignment and loop spans before attempting same-position crossfades. Unrelated tracks are not automatically compatible: use an authored transition or fade where tempo or musical phrasing differs. Automatic time-stretching is not required for 1.0.

### Encounter Selection

| Encounter context | Music response |
| --- | --- |
| Preparation / low threat | Low-intensity arrangement or a restrained preparation cue. |
| Swarmers and Spitters | Normal combat cue; ramp with sustained swarm pressure. |
| Brute or Brood Carrier-led encounter | Heavy encounter cue when these enemies define the wave, rather than for every individual spawn. |
| Burrower breach | Short breach accent and temporary intensity rise; do not interrupt a boss cue. |
| Matriarch | Highest-priority boss cue, held until the encounter ends. |
| Wave cleared / extraction / defeat | Resolve the encounter, then transition to the appropriate low-intensity or ending cue. |

The director evaluates encounter state, not whichever enemy is nearest or currently targeted. In mixed waves, the boss cue takes priority over heavy and normal combat cues. Use a minimum cue hold time, transition cooldown and slower release after danger passes; author their values per cue/encounter during tuning. Cancel stale queued transitions if the relevant encounter has already ended.

Enemy types share these few musical roles. A bespoke soundtrack for every enemy or sector is not required.

### Playback and Acceptance Requirements

* Define cues as data: stable ID, audio assets, intensity variants, tempo, beats per bar, first downbeat, loop boundaries, allowed transition points and encounter tags. Validate required assets and metadata when loading.
* Synchronize against one audio-sample clock, not frame time or separate timers. Start aligned versions at the same musical position and keep them aligned through loops and transitions.
* Preload the audio needed for the next transition. Looping and crossfades must not produce gaps, clicks, doubled beats or accidental loudness spikes.
* Pause/resume must preserve musical position and alignment. Opening settings must not restart playback; if gameplay is paused, the music transport follows that pause. Scene changes must not leave duplicate players running.
* Keep Master, Music and FX gains independent and reactive, including during transitions. Music volume at zero must not lose synchronization. Any temporary ducking must leave the saved volume preference unchanged.
* First validate one low/medium/high cue set and a transition into one second cue, including a boss-priority test. Test repeated loops, fluctuating threat, pause/resume and frame-rate stalls before expanding the soundtrack.

---

# 41. Content Scope Lock

For 1.0:

| Content                  |                  Hard target |
| ------------------------ | ---------------------------: |
| Sectors                  |                        **6** |
| Main environments        | **1 modular kit + variants** |
| Player robot             |                        **1** |
| Player primary weapons   |     **1 core weapon family** |
| Buildable structures     |                        **5** |
| Enemy archetypes         |                        **5** |
| Boss family              |                        **1** |
| In-run tech nodes        |                      **~32** |
| Permanent research nodes |                      **~24** |
| Standard waves/sector    |                       **12** |
| Currencies               |     **Ore + Research Cores** |
| Command Center tabs      |                        **2** |
| Endgame mode             |     **Endless continuation** |
| Narrative cinematics     |                        **0** |

Approved presentation/audio additions: anime-style female pilot portraits for the existing mech, and one shared adaptive music system with a small encounter-based cue set. These do not increase the chassis, enemy or screen counts. Final portrait and track counts are pending asset selection; begin with one portrait and the two-cue audio validation described above.

Anything outside this table should be treated with suspicion.

---

# 42. Things Explicitly Out of Scope

No:

* freeform base construction;
* procedural world generation;
* inventory;
* item rarity;
* loot equipment;
* character classes;
* multiplayer;
* crafting;
* NPC crew management;
* resource chains with multiple materials;
* dialogue trees;
* weapon collection;
* deckbuilding;
* random card drafting;
* second prestige currency;
* New Game+ progression layer;
* huge boss roster;
* multiple mechanically distinct player characters (cosmetic pilot portraits are allowed).

---

# 43. Prototype

The first playable prototype needs only:

**1 map**

**1 robot**

**1 ore deposit**

**1 drill**

**1 turret**

**1 enemy type**

**1 Core**

**4 in-run upgrades**

**3 permanent upgrades**

**3 waves**

It must prove:

> **Mining while hundreds of enemies converge on an increasingly automated operation is fun to watch and fun to manage.**

---

# 44. Prototype Priority

Before producing all assets, test the most technically important question:

# Can the game display and simulate the swarm we are selling?

Stress-test:

* 100 enemies;
* 500;
* 1,000;
* 2,000+;

with projectiles, effects and basic navigation.

The game's marketable feature is not the number of research nodes.

It is the screenshot/video where the player thinks:

> **How the hell am I supposed to survive that?**

---

# 45. Commercial Positioning

Target price:

# **$3.99**

The player should not expect a 30-hour strategy epic.

The pitch should communicate compactness and replayability:

> **Mine an alien world, fortify your operation, and hold back thousands of hostile creatures. Expand toward richer deposits, push your production too far, and turn every failed expedition into permanent research.**

The Steam screenshots should primarily show:

1. early operation;
2. expanding mining network;
3. enormous swarm;
4. heavily upgraded industrial base;
5. terminal research interface.

---

# 46. Final Identity

GIRLS & MINING should feel like someone discovered an unreleased **1980s PC mining-control terminal**, except when the mission begins the terminal becomes an extremely polished top-down action game.

The UI is:

**black, white, gray, functional and severe.**

Orange indicates:

**what the operator is doing.**

Green indicates:

**what has succeeded.**

The world is:

**industrial, hostile and visually restrained until combat tears it apart.**

The progression fantasy is:

> One robot and one drill
> → a mining operation
> → a fortified industrial network
> → a war machine
> → thousands of enemies crashing against it.

And the complete design can be summarized in six words:

# **MINE. EXPAND. FORTIFY. SURVIVE. RESEARCH. REPEAT.**
