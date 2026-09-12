# AFTERGREEN
## Full Game Design Document

**Working title:** AFTERGREEN
**Genre:** Cozy incremental cleanup / automation / ecological restoration game
**Perspective:** 2D top-down, orthographic
**Primary mode:** Single-player
**Business model:** Premium game, $2.99 USD; no microtransactions\
**Status:** Pre-production / concept
**Document purpose:** Define the intended full-game experience within the hard scope in Appendix A. Numerical balance remains subject to validation.

**Scope authority:** [Appendix A: v1.0 Scope Lock](#appendix-a-v10-scope-lock) defines the hard scope for the full game and overrides conflicting content in this document and the vertical slice GDD. Earlier ideas do not authorize additional features or content. Scope changes require explicit approval from the project owner.

---

# 1. High Concept

Humanity is gone.

Centuries of uncontrolled consumption, waste accumulation, and ecological collapse left much of Earth buried beneath garbage.

One environmental restoration robot remains active.

Its directive is simple:

**MAKE THE WORLD GREEN AGAIN.**

The player travels across the ruined planet aboard a strange mobile restoration vessel: part recycling plant, part biodome, part ark.

At each location, the vessel deploys into a temporary cleanup base.

The player:

- vacuums loose garbage,
- excavates buried waste,
- dismantles enormous junk formations,
- recycles recovered material,
- constructs increasingly absurd cleanup machines,
- repairs forgotten robots,
- discovers lost human technology,
- restores local ecology,
- grows a living ecosystem aboard the Ark,
- and eventually departs for another damaged part of the world.

Every place left behind remains improved.

The player does not reset the world.

They move forward.

---

# 2. Player Fantasy

The fantasy progresses through four stages.

### Stage 1 — Tiny robot, impossible mess

The world seems overwhelmingly dirty.

The player manually picks up bottles, cans, paper and scrap.

A single junk pile feels significant.

### Stage 2 — Clever cleaner

The player discovers better tools, learns how junk formations work and begins efficiently dismantling them.

Individual pieces become cascades.

Entire garbage clusters collapse into collectible material.

### Stage 3 — Cleanup engineer

The player begins constructing machines and ecological helpers.

Bots sweep areas automatically.

Birds collect paper.

Wind funnels plastic.

Machines compact debris.

The player stops doing repetitive cleanup and focuses on unusual, valuable and difficult problems.

### Stage 4 — Planetary restorer

The Ark is now a magnificent travelling ecosystem.

Former wastelands are green.

Robot communities populate recovered regions.

The player's cleanup technology operates at ridiculous scale.

The question is no longer:

**"How do I pick up this bottle?"**

It becomes:

**"How do I restore this entire ecosystem?"**

---

# 3. Core Design Pillars

## 3.1 Cleaning must feel good without progression

Vacuuming garbage must already be enjoyable before bonuses, upgrades or automation are introduced.

Trash should:

- move satisfyingly,
- rattle and tumble,
- snap into suction streams,
- produce clear sound and visual feedback,
- create satisfying chain reactions,
- visibly improve the environment.

Upgrades amplify fun.

They must not repair an intentionally unpleasant starting mechanic.

---

## 3.2 Progress must be physically visible

Almost every important progression system should change something the player can see.

Examples:

- the Ark gains modules,
- relics appear installed around the workshop,
- the biodome becomes lush,
- robots move aboard,
- machines remain behind,
- restored regions change on the world map,
- birds and insects appear,
- the player robot gains small permanent attachments.

The world itself is the progress bar.

---

## 3.3 Automation removes chores, not gameplay

Automation should increasingly handle:

- scattered low-value trash,
- transportation,
- sorting,
- repetitive collection.

The player remains responsible for:

- exploration,
- excavation,
- difficult objects,
- relic discovery,
- unusual environmental problems,
- machine placement,
- restoration decisions.

The player should eventually think:

**"I don't need to pick that up anymore; my ecosystem handles it."**

---

## 3.4 The game is cozy, whimsical and slightly melancholic

The extinction of humanity provides context but is not constantly foregrounded.

The present world is hopeful.

Humor primarily comes from:

- obsolete robots,
- absurd garbage,
- machine behaviour,
- animations,
- item descriptions,
- old automated systems still doing pointless jobs.

The game does not mock human suffering.

---

## 3.5 Leaving should feel better than resetting

The prestige loop is represented as **Departure**.

The player does not wipe their accomplishments.

They prepare a region to recover, leave machinery and ecology behind, pack the Ark and travel onward.

Mechanically, temporary progression resets.

Emotionally, the player has accomplished something permanent.

---

# 4. Target Experience

## Audience

Players who enjoy combinations of:

- incremental games,
- cozy games,
- cleanup games,
- light automation,
- collection,
- visible base progression,
- environmental restoration,
- exploration,
- satisfying audiovisual feedback.

The game should remain understandable to players who do not normally play complex factory games.

---

# 5. Platform Direction

Primary development target:

**PC**

Input priorities:

1. Mouse + keyboard
2. Controller

Potential later targets:

- Steam Deck
- console

The UI should therefore avoid interactions requiring pixel-perfect mouse precision.

---

# 6. World Premise

Long after humanity disappears, automated systems continue operating.

Most eventually fail.

A small network of environmental restoration units was created during the final stages of ecological decline.

The player's unit awakens after an unknown period.

Its memory is incomplete.

Its directive remains active.

The player initially understands only:

> REMOVE CONTAMINATION.

Later recovered systems reveal additional directives:

> RESTORE SOIL.
> RESTORE FLORA.
> RESTORE WATER.
> RESTORE FAUNA.
> ESTABLISH SELF-SUSTAINING BIOSPHERE.

The long-term story is not about resurrecting humanity.

It is about creating a world capable of continuing without them.

---

# 7. Creative Differentiation

The premise may remind players of other post-human robot stories, so visual and narrative identity must remain original.

Avoid:

- box-shaped yellow protagonist designs,
- binocular-style eyes,
- beat-for-beat lonely-cleaner storytelling,
- recognizable visual motifs belonging to existing properties.

Possible player silhouette:

- compact circular or beetle-like chassis,
- low tracked or multi-wheel locomotion,
- articulated vacuum trunk,
- seed-shaped glowing core,
- asymmetric salvaged attachments.

The Ark should be visually iconic:

**a mobile greenhouse crossed with a recycling machine and improvised expedition vehicle.**

---

# 8. Macro Game Structure

The world is divided into **Restoration Sites**.

Several sites belong to a larger **Biome**.

The exterior gameplay architecture can reuse one primary game scene populated from biome/site data.

A normal progression flow:

**Ark arrives → deploys → clean → recycle → upgrade → automate → excavate → discover → reach restoration threshold → optionally push milestones → seed ecosystem → pack Ark → depart → choose destination**

A biome may contain approximately:

- 3–6 restoration sites,
- several unique relics,
- one biome-specific ecological mechanic,
- a major recovery milestone.

Completing enough recovery within a biome unlocks new world routes.

---

# 9. Primary Game Spaces

## 9.1 Exterior Restoration Site

The main gameplay space.

Contains:

- Ark/recycling station,
- trash fields,
- junk heaps,
- buried objects,
- local environmental hazards,
- NPC robots,
- relic locations,
- building zones,
- optional discoveries.

The majority of playtime happens here.

---

## 9.2 Ark Biodome Interior

Separate screen.

This is the permanent ecological progression space.

Contains:

- planting areas,
- water habitat,
- soil ecosystem,
- fauna support structures,
- seed archive,
- NPC areas,
- visual trophies from restored biomes.

The dome should feel like home.

Exterior = active, noisy, dirty.

Interior = calm, green, safe.

---

## 9.3 Fabrication / Upgrade Interface

Accessible through the deployed recycler.

Used for:

- temporary run upgrades,
- machine fabrication,
- machine improvements.

Prefer diegetic machinery with a clean overlay rather than a detached RPG menu.

---

## 9.4 Relic Workshop

Permanent technology progression.

Relics physically appear after discovery.

Each major relic may unlock a small technology branch.

---

## 9.5 World Map

Used primarily during Departure.

Shows:

- recovered locations,
- recovering locations,
- available destinations,
- biome branches,
- locked regions and requirements.

---

# 10. Moment-to-Moment Gameplay

The player moves around the exterior environment and identifies useful cleanup targets.

The desired rhythm is:

**spot → approach → manipulate → release → collect → reward**

Example:

A tangled junk mound contains:

- loose bottles,
- paper,
- a tire,
- sheet metal,
- buried electronics.

The player vacuums loose material.

This exposes the tire.

The tire can be latched and pulled free.

Removing it destabilizes the pile.

Several items tumble outward.

The vacuum catches them in a satisfying cascade.

A buried electronic component becomes visible.

Removing it reveals a relic.

One deliberate action has created several rewards.

---

# 11. Player Controls

Conceptual PC controls:

**Movement:** WASD
**Aim:** Mouse or right stick
**Vacuum:** Hold primary action
**Excavate / latch / interact:** Secondary action
**Context interaction:** E / face button
**Machine placement:** Build input
**Return / cancel:** Esc / controller equivalent

Avoid excessive tool swapping.

The vacuum and excavation functionality should ideally exist within one multipurpose restoration arm.

---

# 12. Vacuum System

The vacuum is the player's primary tool.

It creates a suction cone.

Objects have:

- mass,
- suction resistance,
- category,
- material value,
- state.

Possible states:

- loose,
- partially buried,
- anchored,
- tangled,
- frozen,
- floating,
- contaminated.

Loose objects inside the suction field:

1. shake,
2. slide,
3. accelerate,
4. enter a visible suction stream,
5. disappear into the player's hopper.

Vacuum improvements may change:

- width,
- range,
- force,
- maximum collectible mass,
- chain attraction,
- processing efficiency.

---

# 13. Excavation System

Excavation provides contrast to passive vacuuming.

Heavy or buried objects require the player to expose attachment points and apply focused force.

Possible interactions:

- latch and pull,
- rotate free,
- cut binding material,
- lift covering debris,
- excavate sand,
- dismantle attached sections.

Removing structural junk should frequently trigger:

- debris slides,
- object cascades,
- revealed caches,
- hidden pathways,
- relic discoveries.

Heavy objects should feel like small environmental puzzles rather than health bars.

---

# 14. Trash Taxonomy

## Light Debris

Examples:

- paper,
- plastic wrappers,
- cans,
- bottles,
- packaging.

Role:

Fast vacuum satisfaction.

---

## Medium Debris

Examples:

- broken electronics,
- cookware,
- tools,
- small furniture pieces.

Role:

Higher value, stronger suction requirement.

---

## Heavy Debris

Examples:

- tires,
- appliances,
- furniture,
- engine blocks,
- machine components.

Role:

Excavation and manipulation.

---

## Junk Structures

Examples:

- collapsed car piles,
- tangled fencing,
- shipping containers,
- abandoned machines,
- compacted trash walls.

Role:

Multi-stage cleanup encounters.

---

## Comedic Finds

Occasional low-stakes surprises.

Examples:

- suspiciously pristine garden gnome,
- refrigerator containing another refrigerator,
- thousands of identical plastic spoons,
- traffic cone occupying an unreasonable location,
- giant promotional mascot head.

These improve pacing and personality.

---

# 15. Carrying Capacity

The player has a limited hopper.

A full hopper encourages returning to the central recycler.

The intention is to create a natural rhythm:

**explore → fill → return → upgrade → leave again**

Capacity should grow quickly enough that returning does not become tedious.

Automation increasingly bypasses this limitation.

---

# 16. Recycling Station

When deployed, the Ark becomes the center of the local cleanup operation.

Functions include:

- depositing trash,
- displaying recovered tonnage,
- processing material,
- fabricating machines,
- purchasing temporary upgrades,
- installing relic technologies.

Visually, the station expands during a cycle.

Temporary additions may include:

- sorting arms,
- compactors,
- hoppers,
- cables,
- cranes,
- scrap containers.

The station should look increasingly absurd and productive.

---

# 17. Run Resources

Full-game material categories should remain limited.

Suggested initial categories:

### Metal
Used for frames, machines and structural components.

### Polymer
Used for hoses, lightweight machinery and containers.

### Components
Recovered primarily from electronics and specialized junk.

Potential later category:

### Biomass
Used only in biomes where organic processing becomes important.

Avoid excessive crafting currencies.

Trash mass itself remains separate from crafting materials.

---

# 18. Tonnage

Every recovered object contributes real or stylized mass.

The HUD prominently tracks:

**XXX / 1,000 KG RECOVERED**

At:

**1,000 kg**

Departure becomes available.

This is the game's prestige threshold.

Departure is optional.

Players may continue cleaning to reach additional milestones.

Possible extended milestones:

- 1 tonne — Departure unlocked
- 2 tonnes — bonus Seed Fragment
- 4 tonnes — increased ecological reward
- 8 tonnes — rare expedition reward
- biome-specific milestone — cosmetic / relic clue / Ark decoration

Exact values require playtesting.

---

# 19. Temporary Run Upgrades

Purchased using locally recovered material.

Examples:

### Vacuum
- wider intake,
- stronger suction,
- larger hopper,
- longer attraction range,
- lightweight chain collection.

### Mobility
- faster tracks,
- reduced terrain slowdown,
- short boost.

### Processing
- better material yields,
- faster unloading,
- machine efficiency.

### Local automation
- additional bot capacity,
- faster collectors,
- larger machine radius.

These disappear or are converted when departing.

Narrative explanation:

Temporary parts are low-grade field modifications built from local scrap. Before long-distance travel, they are stripped and used to establish the local caretaker infrastructure.

---

# 20. Machines

Machines are fabricated from recycled material.

Machines should be simple to understand visually.

## Collector Bot

Travels around a defined radius collecting lightweight trash.

Deposits at the nearest collection point.

---

## Wind Catcher

Uses environmental wind to funnel light debris toward a pile or machine.

Particularly effective in desert regions.

---

## Compactor

Processes collected trash into dense blocks.

May increase transport or recycling efficiency.

---

## Salvage Drone

Searches exposed junk for high-value components.

---

## Magnetic Collector

Targets metallic debris.

Requires appropriate relic technology.

---

## Sorting Station

Receives mixed trash and increases material yield.

---

## Local Relay

Powers or coordinates nearby machines after the Ark departs.

Machines should avoid factory-level complexity.

No belts requiring exact ratios unless later testing demonstrates demand.

The desired logistics complexity is:

**collector → processor → recycler**

not:

**spreadsheet optimization simulator**

---

# 21. Machine Placement

Machines have:

- simple placement footprint,
- readable effective radius,
- clear input/output behaviour,
- visible animation.

Placement matters because of:

- trash density,
- environmental flow,
- obstacles,
- machine synergy.

The player should be able to understand whether a machine is useful by looking at it.

---

# 22. Relics

Relics are unique permanent discoveries hidden within garbage.

They are found through:

- excavation,
- junk structures,
- optional hidden areas,
- NPC quests,
- biome landmarks.

A relic only needs to be discovered once per save.

Relics should primarily unlock **new capabilities**, not tiny statistical bonuses.

Examples:

### Municipal Routing Chip
Unlocks autonomous collector robots.

### Industrial Magnetic Coupler
Unlocks magnetic machinery.

### Weather Survey Array
Unlocks wind prediction and advanced wind collectors.

### Agricultural Genome Archive
Unlocks advanced biodome flora.

### Hydraulic Excavator Joint
Unlocks stronger heavy-junk manipulation.

### Old Navigation Core
Reveals distant world-map routes.

---

# 23. Relic Technology Trees

A major relic can reveal a permanent mini-tree.

Example:

**Municipal Routing Chip**

Level 1:
Collector Bot blueprint.

Level 2:
Collector Bot returns to local relay automatically.

Level 3:
Bots share discovered trash locations.

Level 4:
Deploy with one basic bot at the start of a restoration site.

Relic trees use permanent resources or milestone unlocks.

They should change behaviour whenever possible.

---

# 24. Biodome

The biodome is the ecological heart of the game.

It starts nearly empty.

Possible starting image:

- cracked planter,
- weak grow lamp,
- tiny patch of living soil,
- one surviving sprout.

Over time it becomes a dense miniature biosphere.

---

# 25. Biodome Progression Categories

## Soil

Examples:

- microbes,
- fungi,
- compost beds,
- fertile substrate.

Gameplay effects:

- improved organic processing,
- degraded-soil biome access,
- faster local ecological recovery.

---

## Flora

Examples:

- grass,
- shrubs,
- flowers,
- cacti,
- trees,
- vines.

Gameplay effects:

- plant-based helpers,
- erosion control,
- seed dispersal,
- ecological route unlocks.

---

## Water

Examples:

- condensation plants,
- reeds,
- pond systems,
- filtration roots.

Gameplay effects:

- wetland restoration,
- water purification,
- access to flooded or aquatic biomes.

---

## Fauna

Examples:

- insect habitats,
- bird perches,
- beetle beds,
- pollinator support.

Gameplay effects:

- animal cleanup helpers,
- seed spreading,
- relic detection,
- specialized passive collection.

---

# 26. Nature-Based Helpers

Nature upgrades should be mechanically readable and humorous.

Examples:

## Paper Birds

Birds gather lightweight paper debris into piles.

Upgrades may cause:

- larger flocks,
- faster gathering,
- delivery directly to collectors.

Visual humor can come from overly organized bird behaviour.

---

## Beetles

Push biodegradable matter toward composting areas.

---

## Magpies

Detect or retrieve shiny metallic objects.

May occasionally become distracted by useless shiny items.

---

## Crows

Reveal suspicious or buried locations.

---

## Fungi

Gradually process organic waste in restored areas.

---

## Reeds

Trap floating debris in water biomes.

Nature should become an alternative form of automation.

---

# 27. Bio-Cores

Every major tonnage threshold produces ecological progression.

At 1 tonne, the recycler creates a **Bio-Core**.

During Departure:

1. the player carries/inserts the Bio-Core into the biodome,
2. chooses an ecological development,
3. the biodome permanently changes,
4. a corresponding restoration package is generated for the current region,
5. the package remains behind as the Ark departs.

This makes the prestige reward both:

- a permanent Ark upgrade,
- and a restoration action affecting the world.

---

# 28. Departure

Departure replaces conventional prestige.

Requirements:

- minimum tonnage reached,
- current site restoration package prepared.

Optional:

- additional milestones,
- unresolved relics,
- NPC interactions.

When the player chooses Departure:

1. local machines finish current tasks,
2. temporary field modifications are recycled into local infrastructure,
3. Ark modules retract,
4. local caretaker relay activates,
5. vegetation begins establishing,
6. Ark packs up,
7. player robot boards,
8. camera shows the recovering site,
9. world map opens.

The sequence should be emotionally satisfying but skippable after repeated viewing.

---

# 29. What Persists

Persistent across departures:

- relic discoveries,
- relic technology,
- biodome ecology,
- Ark modules,
- permanent robot improvements,
- NPC crew,
- discovered blueprints,
- world restoration state,
- milestones,
- cosmetics.

Local / temporary:

- loose crafting materials,
- temporary vacuum upgrades,
- temporary movement upgrades,
- most locally fabricated machines,
- local infrastructure.

Machines are not erased.

They remain at the previous region as part of its recovery system.

---

# 30. Ark / Vessel Progression

The Ark is the player's permanent technological home.

Possible modules:

### Seed Vault
Allows additional ecological species.

### Cargo Bay
Allows one selected machine blueprint or prefabricated machine to travel between regions.

Higher levels increase capacity.

### Scanner Array
Improves relic detection.

### Drone Bay
Provides starting helper bots.

### Water Condenser
Unlocks dry or aquatic restoration routes.

### Climate Shielding
Allows frozen or toxic region travel.

### Navigation Core
Unlocks distant world-map nodes.

### Workshop
Improves permanent technology upgrades.

Every module should visibly alter the Ark.

---

# 31. World Map and Biome Branching

Progression should not be strictly linear.

World routes depend on combinations of:

- biodome ecology,
- relic technologies,
- Ark modules,
- recovered regions.

Example:

**Starting Junk Desert**

Branches toward:

- Dried Seabed if Water ecology is developed,
- Rustbelt Industrial Zone if heavy salvage technology is developed,
- Rocky Highlands if hardy Flora / Soil ecology is developed.

Later routes reconnect.

The player should make meaningful but reversible strategic choices.

No permanent route should become inaccessible forever because of an early decision.

---

# 32. Example Biomes

## 32.1 Sunscar Wastes

Theme:
Trash-filled desert.

Primary mechanic:
Excavation.

Environmental behaviour:
Wind moves paper and lightweight plastics.

Restoration:
Hardy grass, desert shrubs, insect life.

---

## 32.2 Dry Seabed

Theme:
Former ocean floor full of shipping debris and plastic.

Primary mechanic:
Entangled trash.

Features:

- fishing nets,
- old containers,
- boat remains,
- salt flats.

Restoration:
Water channels and salt-tolerant ecology.

---

## 32.3 Rustbelt

Theme:
Industrial ruins.

Primary mechanic:
Heavy dismantling.

Features:

- vehicles,
- machinery,
- metal structures,
- e-waste.

Restoration:
Soil remediation.

---

## 32.4 Flooded Ruins

Theme:
Partially submerged city.

Primary mechanic:
Water currents.

Features:

- floating trash,
- clogged waterways,
- filtration.

Restoration:
Wetlands and aquatic ecology.

---

## 32.5 Frozen Repository

Theme:
Garbage and old technology preserved in ice.

Primary mechanic:
Thawing / extraction.

Restoration:
Cold-resistant ecology.

---

## 32.6 Overgrown Metro

Theme:
Nature has partially recovered without assistance.

Primary mechanic:
Selective cleanup.

The challenge flips:

The player must remove trash **without damaging existing life**.

This creates a late-game inversion of the early "vacuum everything" mentality.

---

## 32.7 E-Waste Badlands

Theme:
Mountains of obsolete electronics.

Primary mechanic:
Electrical hazards and valuable components.

Restoration:
Detoxification and soil recovery.

---

# 33. NPC Robots

Other robots survived because they were:

- sheltered,
- self-maintaining,
- forgotten,
- extremely lucky.

Most continue attempting outdated tasks.

NPCs provide:

- humor,
- light story,
- optional quests,
- technology,
- Ark crew,
- biome personality.

---

# 34. NPC Philosophy

Characters should be memorable through one strong behaviour.

Examples:

### PARKR-7

Ancient parking enforcement robot.

Still tickets buried vehicles.

Sample attitude:

> VEHICLE HAS EXCEEDED ALLOWED PARKING DURATION BY 43,812 DAYS.

Possible permanent function:
Highlights buried vehicles and metallic salvage.

---

### MOP-3

Municipal sanitation unit.

Takes cleaning extremely seriously.

Unlock:
Basic automated sweepers.

---

### B33

Pollination support drone.

Has spent centuries searching for flowers.

Unlock:
Fauna / pollination technologies.

---

### CR8-R

Cargo logistics robot.

Obsessed with loading efficiency.

Unlock:
Cargo Bay improvements.

---

### INFO-9

Tourism information kiosk.

Still gives directions to places that no longer exist.

Mostly comedic.

---

# 35. Robot Crew

Some rescued robots can join the Ark.

Inside the biodome or workshop they:

- move around,
- comment on progress,
- perform little jobs,
- unlock interactions,
- occasionally create emergent comedy.

The Ark gradually becomes a travelling robot community.

---

# 36. Narrative Structure

Story should remain light and discoverable.

Delivery methods:

- environmental details,
- relic descriptions,
- robot dialogue,
- corrupted signage,
- old automated announcements,
- recovered system logs.

Avoid long exposition sequences.

The central emotional arc:

### Beginning
"I clean because my directive says so."

### Middle
"There are other machines. Life may still be possible."

### Later
"We were created because someone hoped the world could recover."

### Ending
The biosphere becomes self-sustaining.

The robot completes its directive.

The final thematic question:

**What does a restoration machine do when the world no longer needs restoring?**

---

# 37. Tone

Target emotional mix:

- 55% satisfying / cheerful,
- 25% whimsical comedy,
- 15% peaceful,
- 5% melancholy.

These percentages are directional, not literal content quotas.

The player should leave sessions feeling hopeful rather than guilty.

---

# 38. Objectives

Avoid quest-log overload.

A restoration site should usually have:

### Primary objective
Reach restoration readiness.

### Secondary objectives
Examples:

- rescue robot,
- uncover relic,
- dismantle landmark,
- restore water route,
- clean specific structure.

### Optional completion
Remove remaining contamination or reach milestone tonnage.

---

# 39. Milestones

Milestones provide long-term goals beyond Departure.

Categories:

### Lifetime recovered mass
1t, 10t, 100t, etc.

### Relics discovered
Technology milestones.

### Biodome diversity
Number of species / habitats.

### Regions restored
World recovery.

### NPCs rescued
Community growth.

### Machine achievements
Automation milestones.

Rewards should favor:

- cosmetics,
- Ark decorations,
- blueprints,
- quality-of-life features,
- optional upgrades.

---

# 40. Economy Principles

The economy should remain comprehensible.

The player should primarily care about three questions:

1. How close am I to my next tonnage milestone?
2. What can I fabricate right now?
3. What permanent improvement will I choose on Departure?

Avoid:

- numerous conversion currencies,
- meaningless +1% bonuses,
- constant inventory management.

---

# 41. Upgrade Philosophy

Prefer behavioural upgrades.

Weak:

> +3% Bird Speed.

Better:

> Birds now carry two paper objects.

Better still:

> Birds now deliver paper directly to collectors.

Statistical upgrades can support progression, but milestone upgrades should visibly change behaviour.

---

# 42. Difficulty Philosophy

No combat is required.

No traditional death state is required.

Environmental obstacles create friction rather than punishment.

Examples:

- mud slows movement,
- tangled nets disable the vacuum briefly,
- electrical zones require insulation,
- ice prevents extraction,
- polluted water blocks certain machines.

Solutions come from preparation and technology.

---

# 43. UI

Main exterior HUD should remain compact.

Core information:

- recovered mass this cycle,
- hopper fill,
- temporary materials,
- current objective,
- contextual interaction prompt.

Potential layout:

Top center:
**742 / 1,000 KG**

Corner:
materials

Near player:
interaction prompts

Avoid permanent minimap clutter unless exploration proves difficult.

---

# 44. Biodome UI

Keep interaction physical where feasible.

Players should:

- approach planting beds,
- see available growth choices,
- confirm one,
- watch the environment change.

The biodome should not simply be a skill-tree screen wearing a garden skin.

---

# 45. Art Direction

## Camera

True or near-true top-down 2D.

Inspired by the readability of classic overhead games without copying their visual design.

Important:

- strong silhouettes,
- readable machine shapes,
- clear ground contrast,
- environmental clutter that remains understandable.

---

## Exterior Palette

Early regions:

- sand,
- faded metal,
- rust,
- dusty plastics,
- muted industrial colors.

Recovered areas introduce:

- saturated greens,
- flowers,
- water,
- soft movement,
- wildlife.

---

## Ark

Should look assembled over centuries from:

- greenhouse architecture,
- salvage machinery,
- expedition vehicle parts,
- irrigation systems.

It should become increasingly lush and eccentric.

---

# 46. Animation Principles

Prioritize animation where it improves responsiveness.

Essential:

- trash shake before suction,
- suction arcs,
- object tumble,
- heavy object strain,
- junk collapse,
- hopper open/close,
- recycler processing,
- machine loops,
- biodome plant growth,
- Ark packing and deployment.

Robot personality can be communicated through body motion more than facial animation.

---

# 47. VFX

Core VFX:

- suction streaks,
- dust puffs,
- recycling sparks,
- material particles,
- small leaves/seed particles,
- machine indicator lights,
- tonnage milestone burst.

Avoid excessive screen-filling effects.

The world should remain cozy and readable.

---

# 48. Audio Direction

Audio is crucial to making cleanup satisfying.

Vacuum sound should respond dynamically to:

- number of objects,
- object mass,
- intake rate.

Trash materials should sound different:

- plastic,
- metal,
- glass,
- paper,
- heavy objects.

Important sounds:

- satisfying intake pop,
- metal rattle,
- compactor thunk,
- relic discovery tone,
- Bio-Core creation,
- Ark machinery,
- tiny robot vocalizations.

---

# 49. Environmental Audio Progression

One long-term reward should be soundscape transformation.

Dirty site:

- wind,
- loose metal,
- machinery,
- electrical hum.

Recovered site:

- grass,
- insects,
- flowing water,
- birds.

Progress can literally sound healthier.

---

# 50. Music

Music should be sparse enough to preserve environmental sound.

Potential approach:

Exterior:
gentle rhythmic mechanical music.

Biodome:
warmer organic instrumentation.

Departure:
short emotional theme.

Recovered sites:
increasing organic layers.

---

# 51. Accessibility

Plan for:

- full input remapping,
- controller support,
- color-independent resource indicators,
- subtitles,
- text size options,
- reduced screen shake,
- reduced particle effects,
- reduced flashing,
- hold/toggle vacuum option,
- aim assistance on controller,
- adjustable suction camera effects.

No core interaction should depend exclusively on color.

---

# 52. Save Structure

Separate persistent and expedition state.

## Persistent

- relics,
- relic upgrades,
- biodome state,
- Ark modules,
- NPC crew,
- map progress,
- milestones,
- discovered technologies.

## Expedition

- current site,
- recovered mass,
- local materials,
- temporary upgrades,
- machine placement,
- current trash state,
- objectives.

Autosave:

- after deposit,
- after relic discovery,
- after permanent upgrade,
- before and after Departure.

---

# 53. Technical Architecture

Recommended high-level scene structure:

### ExteriorGameplayScene
Loads biome/site configuration.

### ArkInteriorScene
Loads persistent biodome state.

### WorldMapScene
Destination selection.

### UI overlays
Recycler, fabrication and relic interfaces.

---

# 54. Trash Performance

The game may eventually display hundreds or thousands of pieces of garbage.

Avoid expensive full physics for every object.

Recommended approach:

- sprite/object pooling,
- simple proximity activation,
- kinematic suction movement,
- lightweight collision,
- physics enabled only during relevant cascades,
- distant garbage merged into decorative clusters where appropriate.

Target:

Stable 60 FPS on reasonable target hardware.

---

# 55. Data-Driven Content

Core content should be defined through editable data objects.

## TrashDefinition
- ID
- sprite
- mass
- material yield
- suction resistance
- interaction type
- sound type
- biome tags

## MachineDefinition
- cost
- footprint
- radius
- target trash types
- speed
- animation
- tech requirement

## RelicDefinition
- unique ID
- discovery text
- unlocked branch
- world appearance

## EcologyDefinition
- biodome category
- visual stages
- permanent effect
- biome requirements

## BiomeDefinition
- environment assets
- trash tables
- hazards
- unique mechanic
- relic pool
- ecological restoration options

---

# 56. Procedural vs Handcrafted Content

Initial recommendation:

**Handcrafted layouts with procedural dressing.**

Handcraft:

- important junk structures,
- relic locations,
- NPC encounters,
- environmental puzzles,
- site landmarks.

Proceduralize:

- loose debris distribution,
- small decorative junk,
- background variation,
- minor resource clusters.

Pure procedural generation is not required for the initial game.

---

# 57. Hard Full-Game Scope

[Appendix A: v1.0 Scope Lock](#appendix-a-v10-scope-lock) is the authoritative full-game scope: four biomes, eight restoration sites, and one short finale, targeting a 3–5 hour first completion.

All content caps, progression limits, and exclusions in the appendix are binding. The earlier larger production targets are superseded. Removing other content does not authorize exceeding a cap or adding an excluded system.

---

# 58. Progression Pacing

Desired first hours:

### First 5 minutes
Cleaning already feels satisfying.

### First 15 minutes
Player understands upgrades.

### First 30 minutes
Player builds automation and performs first Departure.

### First hour
Player sees the permanent biodome effect meaningfully change gameplay.

### First several hours
Multiple ecological and technological strategies emerge.

### Long term
Cleaning occurs at absurd scale.

---

# 59. Endgame

The player eventually gains the ability to restore entire regions using combined ecological and technological systems.

Endgame should not become pure idle gameplay.

The player still solves:

- unique contamination problems,
- mega-structures,
- unusual relic puzzles,
- final biome restoration.

The final objective is establishment of a self-sustaining planetary biosphere.

---

# 60. Ending

When global restoration reaches completion:

The player's original directive changes state.

> BIOSPHERE: SELF-SUSTAINING
> ACTIVE RESTORATION REQUIREMENT: NONE

The game may allow continued free play afterward.

Potential final image:

The Ark stands somewhere green.

Its engines are quiet.

Robots and wildlife continue around it.

For the first time, the player has nothing urgent to clean.

---

# 61. Major Design Risks

## Risk: Vacuuming becomes repetitive

Mitigation:

- physical cascades,
- varied trash states,
- excavation,
- absurd objects,
- rapid behavioural upgrades.

---

## Risk: Automation removes gameplay

Mitigation:

Automate low-interest work while increasing player focus on high-value problems.

---

## Risk: Prestige feels like loss

Mitigation:

Departure leaves physical accomplishments behind and immediately grants permanent ecological growth.

---

## Risk: Too many progression systems

Mitigation:

Keep each layer distinct.

**Run:** recycled material.

**Relics:** technology.

**Biodome:** ecology.

**Ark:** travel / capacity.

---

## Risk: Scope explosion

Mitigation:

Do not build full-game systems until the vertical slice proves the five core interactions.

---

# 62. Five Questions the Vertical Slice Must Answer

1. Is vacuuming trash enjoyable for several minutes without relying on rewards?
2. Does excavation add meaningful variation rather than interrupting flow?
3. Does automation produce a satisfying "I built this" feeling?
4. Does reaching 1 tonne create anticipation?
5. Does planting the biodome and departing feel rewarding enough that losing temporary upgrades feels acceptable?

If the answer to any of these is consistently "no," solve that before expanding the game.

---

# 63. One-Sentence Pitch

**AFTERGREEN is a cozy incremental cleanup game where a forgotten restoration robot travels across a trash-covered post-human Earth in a mobile biodome, turning waste into machines, recovering lost technology, growing a travelling ecosystem, and leaving each ruined region greener than it found it.**

---

# 64. Core Loop Summary

**VACUUM**

↓

**EXCAVATE**

↓

**RECYCLE**

↓

**UPGRADE**

↓

**BUILD**

↓

**AUTOMATE**

↓

**DISCOVER RELICS**

↓

**1 TONNE**

↓

**GROW BIODOME**

↓

**RESTORE SITE**

↓

**DEPART**

↓

**NEW BIOME**

↓

**DO IT BETTER, BIGGER AND WEIRDER**

---

# Appendix A: v1.0 Scope Lock

**Document type:** Production scope appendix\
**Target price:** $2.99 USD\
**Purpose:** Lock the content and progression scope for the full AFTERGREEN game. These limits and exclusions are binding and override conflicting earlier design content.

---

## A.1 Scope Principle

AFTERGREEN should feel like a **small, complete premium game**, not a prototype stretched with grind.

The target is a compact incremental restoration game with:

- a satisfying cleanup loop,
- visible incremental acceleration,
- a meaningful prestige/departure cycle,
- a cozy permanent biodome,
- enough biome variety to feel like a journey,
- and a clear ending.

All implementation must stay within the systems, content caps, and exclusions below. Do not add features beyond this scope. Equivalent-scope substitutions do not authorize exceeding caps or introducing excluded systems. Only an explicit scope revision approved by the project owner can change these limits.

---

## A.2 Target Playtime

| Player Type | Target |
|---|---:|
| First completion | **3–5 hours** |
| Completionist | **5–7 hours** |
| Vertical slice/demo | **20–30 minutes** |

The game should not rely on excessive grind to reach these targets.

---

## A.3 Full Game Content Cap

| System | v1.0 Scope |
|---|---:|
| Biomes | **4** |
| Restoration sites | **8 total** |
| Sites per biome | **2** |
| Finale | **1 short ending sequence** |
| Vacuum models | **4** |
| Chassis models | **4** |
| Local upgrade categories | **3** |
| Upgrade tracks per category | **3** |
| Ranks per local upgrade track | **5** |
| Cleanup machine types | **5** |
| Unique relics | **8** |
| Biodome branches | **4** |
| Biodome upgrades per branch | **4** |
| Total biodome upgrades | **16** |
| Ark visual tiers | **4 total** |
| Major robot NPCs | **6** |
| Recruitable Ark residents | **4–5** |
| Lifetime milestones | **12** |
| Spendable currencies | **3 maximum** |
| Main gameplay screens | **4 maximum** |

These are **hard production caps** for v1.0.

---

# A.4 Core Progression Structure

The player progresses at four different scales.

## Moment-to-Moment

**Find trash → vacuum/excavate → collect**

This must feel satisfying without relying on progression rewards.

---

## Incremental Loop

**Fill capacity → return to Ark → recycle → buy upgrades → go back out stronger**

The game does not need a separate formal "run" system.

A normal outing is naturally limited by robot carrying capacity and the player's decision to return.

---

## Regional Loop

**Upgrade → automate → discover → reach restoration threshold → restore → depart**

One restoration site is effectively the game's larger run.

Departure is the prestige/reset event.

---

## Meta Loop

**Grow Biodome → improve Ark → unlock routes → restore Earth**

This progression persists permanently.

---

# A.5 Local Upgrade Scope

Each restoration site contains three primary upgrade groups.

## Vacuum

Answers:

> How quickly and effectively can I collect trash?

Scope:

- **3 upgrade tracks**
- **5 ranks per track**
- **15 total Vacuum ranks available per site**

Possible future track themes include:

- suction,
- intake width,
- heavy-object capability.

Exact upgrade effects are not locked by this appendix.

---

## Chassis

Answers:

> How much work can my robot perform before returning?

Scope:

- **3 upgrade tracks**
- **5 ranks per track**
- **15 total Chassis ranks available per site**

Possible future track themes include:

- carrying capacity,
- mobility,
- terrain capability.

---

## Operation / Recycling

Answers:

> How productive is the entire cleanup operation?

Scope:

- **3 upgrade tracks**
- **5 ranks per track**
- **15 total Operation ranks available per site**

Possible future track themes include:

- resource recovery,
- fabrication efficiency,
- automation capacity.

---

## Local Upgrade Total

Each site can therefore expose up to:

**45 local upgrade ranks**

The player is **not expected to max every rank** before departing.

A normal restoration site should result in approximately:

**15–25 purchased local upgrades**

This keeps the pace active without requiring full completion of every branch.

---

# A.6 Major Equipment Progression

Local upgrade ranks provide frequent incremental purchases.

Major equipment provides larger milestone moments.

## Vacuum Models

Hard cap:

**4 total vacuum models**

Structure:

```text
Vacuum I
↓
Vacuum II
↓
Vacuum III
↓
Vacuum IV
```

A new model should:

- create a clear visual upgrade,
- meaningfully change cleanup capability,
- feel more significant than ordinary tuning ranks.

Do not create large inventories of sidegrade vacuums for v1.0.

---

## Chassis Models

Hard cap:

**4 total chassis models**

Structure:

```text
Chassis I
↓
Chassis II
↓
Chassis III
↓
Chassis IV
```

A chassis model should visibly evolve the player's robot and provide a meaningful capability milestone.

Do not turn chassis progression into an equipment-loot system.

---

# A.7 Cleanup Machines

Hard cap:

**5 machine types**

Each machine must have a clearly different job.

Machines should provide increasing automation without turning the game into a factory-management simulator.

Desired complexity:

```text
Collector
↓
Processor / Specialist
↓
Recycler / Ark
```

Avoid:

- conveyor-belt networks,
- ratio optimization,
- large machine tech trees,
- dozens of machine variants.

Machine scaling should mostly come from the shared Operation progression and relic unlocks.

---

# A.8 Relics

Hard cap:

**8 unique relics**

Target distribution:

**2 per biome**

Relics are permanent discoveries.

Each relic should ideally unlock or substantially alter something, such as:

- a machine category,
- vacuum capability,
- Ark system,
- automation behavior,
- technology branch,
- route or traversal capability.

Relics should **not** primarily be minor percentage bonuses.

Finding one should feel important.

---

# A.9 Biodome Progression

The Biodome is the main permanent ecological progression system.

Hard cap:

**4 branches × 4 upgrades = 16 upgrades**

Current branch structure:

- **Flora**
- **Soil**
- **Water**
- **Fauna**

Each upgrade should:

- visibly change the Biodome,
- provide a meaningful permanent benefit,
- ideally affect future cleanup or restoration.

The Biodome is not a separate gardening simulation.

Do not add:

- crop timers,
- freeform farming,
- large inventories of plant species,
- maintenance chores.

The Biodome should remain a **visualized permanent skill tree**.

---

# A.10 Ark Progression

The Ark should evolve visually without becoming another giant upgrade system.

Hard cap:

**4 visual tiers total**

Suggested structural pacing:

```text
Tier 1 — Broken restoration vehicle
Tier 2 — Functional mobile recycler
Tier 3 — Established travelling biodome
Tier 4 — Endgame ecological Ark
```

Ark functionality should primarily be unlocked by:

- relics,
- Biodome progress,
- major milestones.

Do not add a second large Ark-only upgrade tree in v1.0.

---

# A.11 Biome Scope

Hard cap:

**4 biomes**

Each biome contains:

- **2 restoration sites**
- **1 primary gameplay/environmental twist**
- **2 relics**
- **1 strong visual identity**
- **1 restoration identity**

Total:

**8 restoration sites**

Recommended structure:

```text
Biome 1
├─ Site 1
└─ Site 2

Biome 2
├─ Site 3
└─ Site 4

Biome 3
├─ Site 5
└─ Site 6

Biome 4
├─ Site 7
└─ Site 8

Finale
```

Biome variety should modify the existing game rather than rebuild it.

A useful production target is:

**70–80% shared systems/assets**

with

**20–30% biome-specific content**

per biome.

---

# A.12 Restoration Site Duration

Target site duration:

**20–30 minutes**

The first site may be closer to:

**25–30 minutes**

Later sites should remain similar or slightly faster despite containing harder content, because permanent progression should increase player efficiency.

With 8 sites, this gives approximately:

**3–4 hours of exterior gameplay**

before adding:

- Biodome time,
- Ark interactions,
- upgrade decisions,
- robot dialogue,
- relic discovery,
- departure sequences,
- finale.

This supports the target **3–5 hour first completion**.

---

# A.13 Robot NPC Scope

Hard cap:

**6 major robot NPCs**

Each major NPC should have:

- one strong visual identity,
- one clear personality hook,
- several dialogue states,
- a mechanical or world connection.

Approximately:

**4–5** may eventually live aboard or around the Ark.

Do not build a large RPG cast.

Quality and memorability matter more than quantity.

---

# A.14 Milestones

Hard cap:

**12 lifetime milestones**

Milestones may track:

- total recovered mass,
- restored regions,
- relic discoveries,
- Biodome progress,
- machines built,
- NPCs rescued.

Rewards should mostly include:

- cosmetics,
- Ark decorations,
- small quality-of-life benefits,
- achievements.

Avoid introducing another large progression currency solely for milestones.

---

# A.15 Economy Scope

Maximum:

**3 spendable currencies**

Suggested structure:

## Scrap

Common local progression resource.

Used for:

- local upgrades,
- common fabrication,
- basic machinery.

---

## Components

Rarer technological resource.

Used for:

- equipment milestones,
- advanced machinery,
- technology-related upgrades.

---

## Bio-Core / Restoration Resource

Permanent ecological progression resource.

Used for:

- Biodome development,
- major restoration choices.

---

## Tonnage Is Not Currency

Recovered kilograms/tonnes are progression.

Example:

```text
742 / 1,000 KG
```

Tonnage determines restoration readiness and milestones.

It should not become another spendable resource.

---

# A.16 UI / Screen Scope

Main gameplay screens are capped at:

**4**

## 1. Exterior

Primary cleanup gameplay.

---

## 2. Workshop

Contains:

- Vacuum upgrades,
- Chassis upgrades,
- Operation upgrades,
- machine fabrication,
- equipment milestones.

---

## 3. Biodome

Permanent ecological progression.

---

## 4. World Map

Used for:

- departure,
- destination choice,
- restored-region overview.

Relics should preferably live inside an existing screen or small overlay rather than creating a fifth major gameplay screen.

Main menu, settings, credits and similar utility screens are not included in this cap.

---

# A.17 Deliberately Excluded from v1.0

The following systems are explicitly **out of scope**. Replacing another feature does not authorize adding them.

- Combat
- Health/damage system
- Hunger or survival mechanics
- Battery-as-run-timer system
- Randomized equipment affixes
- Equipment loot rarity
- Large crafting inventory
- Complex recipe chains
- Many material types
- Procedural world generation
- Day/night gameplay system
- Weather simulation
- Freeform factory belts
- Online multiplayer
- Online leaderboards
- Large robot companion roster
- Full relationship system
- Ark interior decoration system
- Freeform base building
- Gardening minigame
- Farming simulation
- Endless mode required for launch
- New Game+ required for launch
- Post-game prestige layer above global restoration
- Live-service systems
- Daily quests
- Microtransactions

---

# A.18 What Resets and What Persists

## Returning to the Ark During a Site

Nothing meaningful resets.

The player:

- unloads collected trash,
- receives resources,
- buys upgrades,
- leaves again.

Local upgrades remain active.

---

## Departure / Prestige

Departure ends the current restoration site.

### Stays Behind / Resets

- local upgrade ranks,
- local cleanup machinery,
- local material stockpile,
- temporary site infrastructure.

In fiction, these systems are left behind to continue restoring the region.

### Persists

- Vacuum model,
- Chassis model,
- relic discoveries,
- relic technologies,
- Biodome progression,
- Ark progression,
- NPC crew,
- world restoration state,
- milestones.

Departure should feel like **completion and movement forward**, not deletion.

---

# A.19 Progression Cadence Target

The intended reward rhythm is:

## Every few seconds

Satisfying trash pickup.

---

## Every 1–3 minutes

Local upgrade purchase.

---

## Every 5–10 minutes

Meaningful discovery, automation improvement or major target.

---

## Every 20–30 minutes

Site restoration and Departure.

---

## Every 1–2 sites

Major Vacuum, Chassis, Relic or Ark milestone.

---

## Approximately every hour

Noticeable biome/world progression.

This cadence is more important than raw upgrade count.

---

# A.20 Scope Rule

The following sentence should be treated as the v1.0 production rule:

> **AFTERGREEN 1.0 contains four biomes, eight restoration sites, four Vacuum tiers, four Chassis tiers, five cleanup machines, eight relics, sixteen Biodome upgrades, four Ark visual tiers, six major robot NPCs, twelve milestones and no more than three spendable currencies. No system may exceed these caps, and no additional or excluded systems may be added. Removing other content does not create an exception.**

---

# A.21 Production Priority

If schedule pressure appears, cut in this order:

1. optional cosmetic rewards,
2. milestone complexity,
3. secondary NPC dialogue,
4. site-specific decorative content,
5. minor biome-specific variations,
6. optional relic interactions.

Do **not** cut before validating:

- vacuum feel,
- capacity/return loop,
- local incremental upgrading,
- excavation,
- automation,
- one-tonne restoration goal,
- Biodome payoff,
- Departure.

These systems define the game.

---

# A.22 v1.0 Success Definition

AFTERGREEN is complete when the player can:

1. begin with a weak restoration robot,
2. feel themselves become dramatically more productive,
3. upgrade their Vacuum and Chassis across the game,
4. build increasingly useful cleanup automation,
5. discover all eight relics,
6. develop a visible Biodome,
7. restore eight ruined sites across four biomes,
8. grow the Ark into its final form,
9. reach a clear planetary-restoration finale,
10. finish feeling that they played a small but complete game.

That is the intended $2.99 product.
