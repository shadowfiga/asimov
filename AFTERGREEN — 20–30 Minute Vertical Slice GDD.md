# AFTERGREEN
## 20–30 Minute Prototype / Vertical Slice GDD

**Objective:** Build the smallest polished playable experience capable of proving whether AFTERGREEN's central loop is fun.

This is not intended to prove the entire progression system.

This build should answer:

> **Does cleaning trash, uncovering larger objects, building automation, reaching one tonne, growing the biodome and leaving for a new region make the player want to do it again?**

Target first-playthrough duration:

**25 minutes**

Acceptable range:

**20–30 minutes**

---

# 1. Vertical Slice Design Hypotheses

The slice exists to test six hypotheses.

### H1 — Vacuuming is inherently satisfying

A player should enjoy collecting loose trash before receiving meaningful upgrades.

### H2 — Excavation prevents monotony

Heavy objects and junk structures should create moments of anticipation and payoff.

### H3 — Automation creates delight

The first helper machine should noticeably change how the player thinks about cleaning.

### H4 — Progress accelerates

The second half of the cycle should feel meaningfully faster and more powerful than the first.

### H5 — 1 tonne is a motivating target

Players should notice the number climbing and understand why reaching 1,000 kg matters.

### H6 — Departure feels like advancement, not deletion

After losing temporary upgrades, the biodome reward and new destination should make the player excited to continue.

---

# 2. Slice Structure

The vertical slice contains:

## Exterior Site A
Junk Desert.

Approximately 20–24 minutes.

## Biodome
Approximately 2–3 minutes.

## Departure
Approximately 30–60 seconds.

## Exterior Site B Teaser
Approximately 2–4 minutes.

The player should see their first permanent ecological upgrade affecting Site B before the demo ends.

---

# 3. Scope Boundaries

## INCLUDE

- player movement,
- vacuum,
- suction physics,
- hopper,
- recycler,
- 3 temporary upgrades,
- excavation,
- 2 junk structures,
- recovered tonnage,
- one relic,
- one permanent relic unlock,
- one cleanup machine,
- one NPC robot,
- one Bio-Core,
- biodome screen,
- two ecological choices,
- Departure sequence,
- second-site teaser,
- humor,
- polished sound and VFX.

---

## DO NOT INCLUDE

- full world map,
- procedural generation,
- multiple complete biomes,
- complex material sorting,
- combat,
- hunger / battery survival,
- day/night cycle,
- dynamic weather,
- full NPC dialogue trees,
- multiple relic trees,
- advanced logistics networks,
- extensive Ark customization,
- crafting inventory,
- quests beyond one simple NPC event,
- large skill trees.

If schedule becomes tight, cut features from this excluded list before degrading vacuum polish.

---

# 4. Prototype Economy Simplification

The full game may eventually separate:

- metal,
- polymer,
- components.

The vertical slice should use:

### Scrap
Common temporary currency generated from recycled trash.

### Components
Rare temporary resource recovered from electronics / specific objects.

This prevents economy complexity from obscuring the core gameplay test.

Tonnage remains separate.

---

# 5. Site A — Visual Concept

Environment:

A circular/irregular desert restoration zone surrounding the deployed Ark.

Visual layers:

### Center
Ark / recycler.

### Inner ring
Loose beginner trash.

### Mid ring
Moderate garbage, first heavy objects.

### Outer ring
Dense junk structures, NPC and relic.

### Boundaries
Huge inaccessible garbage fields implying a much larger world.

The site should visually communicate:

**You are a tiny machine surrounded by an absurd amount of waste.**

---

# 6. Approximate Site A Layout

```text
        [ dense junk wall ]

 [Heap A]        [NPC Area]

       \          /
        \        /

   loose + medium trash

         [ ARK ]
          [🤖]

     beginner trash

      [Heap B / Relic]

        [boundary]
```

The Ark should always be reasonably easy to locate.

The player should rarely spend more than 10–15 seconds travelling without interacting with something.

---

# 7. Site Trash Budget

Target total recoverable mass:

**~1,300 kg**

This allows:

- Departure at 1,000 kg,
- optional continued cleanup,
- enough remaining material that machines can continue operating.

Suggested composition:

### Loose debris
~220 objects  
~220 kg total

Examples:

- bottles,
- cans,
- paper,
- plastic.

### Medium debris
~80 objects  
~240 kg total

Examples:

- electronics,
- small metal parts,
- containers.

### Heavy objects
~18 objects  
~270 kg total

Examples:

- tires,
- chairs,
- appliance shells.

### Junk structures
6 larger clusters  
~570 kg combined

Total:

**~1,300 kg**

These values are tuning starting points.

---

# 8. Player Starting State

The player begins with:

### Vacuum
Short range, but already pleasant.

### Hopper
100 kg capacity.

### Excavation latch
Available from the start.

Do not lock the interesting mechanic behind ten minutes of progression.

### Movement
Comfortable base speed.

The starting robot should not feel deliberately crippled.

---

# 9. Vacuum Behaviour

Hold primary input.

Objects inside the vacuum cone:

1. shake,
2. rotate slightly,
3. slide toward the player,
4. accelerate,
5. enter suction stream,
6. trigger impact/intake sound,
7. increase hopper and tonnage preview.

The vacuum should be capable of pulling several light objects simultaneously.

Target feeling:

**"I want to sweep this whole patch."**

---

# 10. Vacuum Juice Requirements

Minimum polish:

- animated suction cone,
- small dust particles,
- object shake,
- acceleration curve,
- intake sparks/puffs where appropriate,
- individual material sounds,
- player recoil or tiny chassis reaction,
- controller vibration if available,
- hopper gauge bounce,
- tonnage counter punch animation.

This should receive more polish time than most other systems.

---

# 11. Hopper

Starting capacity:

**100 kg**

When full:

- vacuum stops accepting new mass,
- clear visual/audio indicator,
- recycler waypoint briefly appears.

The player returns to the Ark.

Deposit duration target:

**1–2 seconds**

Deposit should feel satisfying:

- hatch opens,
- trash transfers rapidly,
- recycler shakes,
- mass counter climbs,
- Scrap is produced.

Avoid long deposit animations.

---

# 12. Temporary Upgrades

Only three are required.

## Upgrade 1 — Wider Intake

Cost:
Low enough to purchase after first or second deposit.

Effect:
~30–40% wider suction cone.

Purpose:
Immediate visible power growth.

---

## Upgrade 2 — Bigger Hopper

Effect:

100 kg → 160 kg.

Purpose:
Reduce travel friction.

---

## Upgrade 3 — Stronger Motor

Effect:
Allows easier collection of medium objects and increases pull speed.

Purpose:
Previously inconvenient objects become trivial.

No additional upgrade tree is necessary.

---

# 13. Intended Upgrade Timing

Target:

**2–4 minutes**
First upgrade.

**6–8 minutes**
Second upgrade.

**10–14 minutes**
Third upgrade or machine investment.

If players routinely go longer than five minutes without buying anything, economy tuning is too slow.

---

# 14. Excavation Interaction

The player encounters partially buried heavy objects.

When aiming at one:

Prompt:

**LATCH**

Holding secondary input connects the restoration arm.

Player movement now applies pull force.

The object:

- shifts,
- creates dust,
- produces strain audio,
- gradually loosens.

When released:

- object pops free,
- nearby debris tumbles,
- hidden items become visible.

Target duration:

**2–5 seconds**

Avoid progress bars where possible.

The movement of the object itself communicates progress.

---

# 15. Junk Structure A — Tire Anchor

Purpose:

Teach excavation.

Composition:

- surface bottles,
- paper,
- old tire,
- small junk cavity.

Flow:

1. player vacuums loose trash,
2. tire becomes exposed,
3. player pulls tire,
4. pile collapses,
5. high-value electronics emerge.

Reward:

Components.

---

# 16. Junk Structure B — Relic Heap

Purpose:

Create first major discovery.

Composition:

- scrap panels,
- appliance shell,
- buried municipal robot hardware,
- relic.

The player must remove two structural pieces.

Then the heap partially collapses.

Inside:

**MUNICIPAL ROUTING CHIP**

---

# 17. Relic Discovery

Target discovery timing:

**7–10 minutes**

Sequence:

- short visual pulse,
- unique sound,
- camera emphasis for <1 second,
- item card.

Text:

**MUNICIPAL ROUTING CHIP**

> Designed to coordinate municipal maintenance units.  
> Somehow still believes the city is operating normally.

Permanent effect:

**AUTOMATION TECHNOLOGY UNLOCKED**

The relic is permanently registered immediately.

---

# 18. Collector Bot

The Routing Chip unlocks one fabrication recipe.

## Collector Bot

Cost:

Moderate Scrap + 1 Component.

Player places it near a trash-rich region.

Behaviour:

1. identifies loose trash,
2. drives toward it,
3. picks it up,
4. stores limited mass,
5. returns to recycler,
6. dumps trash,
7. repeats.

Do not require path programming.

---

# 19. Collector Bot Personality

The bot should be funny through behaviour.

Possible traits:

- excited beep when spotting trash,
- tiny reverse beep despite being extremely small,
- occasionally spins after depositing,
- may struggle comically with one object before succeeding.

It should become immediately endearing.

---

# 20. Automation Proof

Within approximately one minute of deployment, the player should visibly see:

- bot finds garbage,
- bot collects it,
- bot drives home,
- tonnage increases.

The player should realize:

**"That area is being cleaned without me."**

This is one of the most important moments in the slice.

---

# 21. NPC Encounter

NPC:

**PARKR-7**

Location:

Beside a half-buried wrecked vehicle.

Behaviour:

Repeatedly attempts to issue a parking citation.

Example dialogue:

> VIOLATION DETECTED.

> PARKING LIMIT: 2 HOURS.

> CURRENT DURATION: 43,812 DAYS.

> PENALTY CALCULATION FAILED.

Player must clean / excavate around the NPC to free it.

---

# 22. NPC Reward

For the slice, keep the reward simple.

PARKR-7 scans the environment and marks a nearby high-value metal object.

Then:

> PUBLIC SERVICE RESUMED.

The robot may remain at the site.

Do not build full crew recruitment yet.

The interaction exists to test whether robot humor fits the world.

---

# 23. Site Progression Timeline

## 0:00–1:00 — Wake / Orientation

Minimal intro.

Player robot activates beside Ark.

HUD appears.

Objective:

**RECOVER 1,000 KG**

Nearby trash creates an obvious cleaning opportunity.

No long cutscene.

---

## 1:00–4:00 — First Cleaning

Player learns:

- movement,
- vacuum,
- hopper,
- deposit.

Target:

First upgrade purchased.

Fun question:

Does sweeping garbage already feel satisfying?

---

## 4:00–7:00 — Excavation

Player reaches Tire Anchor.

Learns:

- latch,
- pull,
- cascade.

Receives first Components.

Target:

Player understands that garbage piles contain more than surface trash.

---

## 7:00–10:00 — Relic Discovery

Player dismantles Relic Heap.

Finds Routing Chip.

Returns to Ark.

Collector Bot recipe unlocks.

---

## 10:00–14:00 — First Automation

Player builds Collector Bot.

Places it.

Bot begins operating.

Meanwhile player cleans elsewhere.

Target:

First clear parallel-progress moment.

---

## 14:00–18:00 — Power Acceleration

Player now has:

- upgraded vacuum,
- larger hopper,
- automation.

Trash that felt significant earlier should disappear rapidly.

Player rescues PARKR-7.

---

## 18:00–23:00 — Big Cleanup Payoff

Player attacks largest junk cluster.

Includes:

- several heavy pieces,
- large cascade,
- high tonnage reward.

Collector Bot contributes in parallel.

Recovered mass approaches 1 tonne.

---

## 22:00–25:00 — Departure Ready

Counter reaches:

**1,000 KG**

Everything briefly reacts.

Ark produces a Bio-Core.

Message:

**RESTORATION READY**

Subtext:

**You may depart now, or continue recovering material.**

Player is directed toward biodome.

---

## 24:00–27:00 — Biodome

Player enters Ark interior.

Atmosphere changes.

Outside:

mechanical + dusty.

Inside:

quiet + warm.

The biodome initially contains:

- one patch of soil,
- one weak plant,
- one empty ecological bed.

Player inserts Bio-Core.

Two permanent ecological choices appear.

---

## 27:00–28:00 — First Ecology Choice

Choice A:

### PAPER FINCH HABITAT

Small birds will gather paper trash into piles in future sites.

Choice B:

### SCRUB GRASS

Lightweight trash caught in vegetation becomes easier to collect and restored sites recover faster.

For the slice, both should cause an obvious visual dome change.

Recommended default test choice:

Paper Finch.

It provides a very visible Site B payoff.

---

## 28:00–29:00 — Departure

Player confirms Departure.

Sequence:

- temporary recycler attachments retract,
- local Collector Bot remains,
- small local restoration beacon activates,
- Ark closes,
- player boards,
- new growth appears around the old site,
- Ark moves away.

Message:

**SITE RECOVERY INITIATED**

Temporary upgrades reset.

Relic and biodome unlock remain.

---

## 29:00–30:00+ — Site B Teaser

Ark arrives in another desert subregion with different dressing.

The player exits.

Paper is visible nearby.

If Paper Finch Habitat was selected:

birds immediately gather several sheets into a pile.

The player vacuums them in one satisfying sweep.

Message:

**NEW RESTORATION SITE**

Fade/end demo.

This proves permanent progression.

---

# 24. Bio-Core Interaction

The Bio-Core should feel physically valuable.

Presentation:

- created inside recycler,
- glowing biological/mechanical object,
- robot physically carries or transfers it,
- inserted into biodome pedestal.

The player should understand:

**trash has literally been converted into life.**

---

# 25. Biodome Slice Layout

Only one small room is required.

Contents:

- entrance,
- Bio-Core pedestal,
- one central planter,
- one empty habitat location,
- window showing exterior or Ark structure.

No freeform decorating.

No full gardening simulation.

---

# 26. Biodome Atmosphere

Must strongly contrast exterior.

Exterior audio:

- wind,
- scrap rattling,
- motors.

Interior audio:

- soft ventilation,
- water droplets,
- gentle plant rustling,
- eventually birds.

Lighting:

warmer and calmer than exterior.

The player should immediately understand this is their home.

---

# 27. Temporary vs Permanent Clarity

The player must understand what Departure does.

Before confirming:

Display compact summary:

**STAYS HERE**
- temporary field upgrades,
- Collector Bot,
- local recovery machinery.

**TRAVELS WITH YOU**
- Municipal Routing Chip,
- automation blueprint,
- biodome upgrade.

Avoid saying "you will lose everything."

The fiction is:

**you are leaving tools behind to finish the job.**

---

# 28. Material Balance Starting Values

Use these only as initial tuning targets.

### Light object
Mass:
0.5–2 kg

Scrap:
1–2

### Medium object
Mass:
2–6 kg

Scrap:
2–4

### Heavy object
Mass:
10–30 kg

Scrap:
5–10

### Electronics
Adds:
chance / guaranteed Component.

### Junk structure
Mass reward:
50–120 kg total.

Exact values should be editable without code.

---

# 29. Tonnage Pacing Target

Desired cumulative mass:

### Minute 5
~150–250 kg

### Minute 10
~350–450 kg

### Minute 15
~550–700 kg

### Minute 20
~800–900 kg

### Minute 23
~1,000 kg

The curve should accelerate.

If progress remains linear, upgrades are not feeling powerful enough.

---

# 30. Collector Bot Contribution

During the first cycle:

Target contribution:

**10–20% of total recovered tonnage after construction**

Enough to matter.

Not enough to play the game by itself.

---

# 31. Movement Pacing

Time from outer active area to recycler:

Target:

**<15 seconds**

After hopper upgrade:

Fewer total return trips should compensate for site expansion.

Do not make walking back to base the dominant activity.

---

# 32. Site Density

At almost all times, the player should see at least one of:

- collectible trash,
- a junk structure,
- machine activity,
- landmark,
- NPC,
- interesting buried object.

Large empty travel spaces do not belong in the slice.

---

# 33. Camera

Top-down orthographic.

Requirements:

- player centered with slight aim bias optional,
- enough zoom to see incoming cleanup targets,
- trash remains readable,
- Ark recognizable from nearby regions.

Do not implement elaborate cinematic camera behaviour.

Use brief emphasis only for:

- relic,
- Bio-Core,
- Departure.

---

# 34. Art Asset List

## Player

- idle,
- move,
- vacuum,
- latch/pull,
- deposit,
- carry Bio-Core.

---

## Ark Exterior

- deployed base,
- recycler animation,
- Bio-Core production,
- departure configuration.

---

## Ark Interior

- room background,
- planter,
- Bio-Core pedestal,
- initial plant,
- Finch Habitat,
- Scrub Grass state.

---

## Trash

Minimum visual set:

- 4 paper/plastic pieces,
- 4 cans/bottles,
- 4 metal scraps,
- 3 electronics,
- tire,
- chair,
- appliance,
- vehicle shell,
- sheet metal,
- junk-pile cluster pieces.

Reuse with rotation/scaling where appropriate.

---

## Machines

Collector Bot only.

Required animations:

- idle,
- drive,
- collect,
- carry,
- deposit.

---

## NPC

PARKR-7.

Animations:

- idle scanning,
- ticket print,
- freed reaction.

---

## Environment

- sand tiles,
- cracked ground,
- trash stains,
- fencing,
- scrap walls,
- rocks,
- dead vegetation,
- site boundary clutter.

---

# 35. VFX Asset List

Minimum:

- suction particles,
- dust,
- trash intake pop,
- deposit burst,
- junk collapse dust,
- relic reveal,
- Bio-Core glow,
- tonnage milestone burst,
- seed/growth effect,
- departure dust trail.

---

# 36. Audio Asset List

Critical:

- vacuum idle,
- vacuum active,
- vacuum loaded variation,
- paper intake,
- plastic intake,
- metal intake,
- heavy scrape,
- latch,
- heavy object release,
- recycler deposit,
- compactor/recycler processing,
- Collector Bot movement,
- Collector Bot chirps,
- relic sting,
- Bio-Core creation,
- biodome ambience,
- plant growth sound,
- Ark departure.

Music can initially consist of:

- one exterior track,
- one biodome loop,
- one short departure cue.

---

# 37. UI Asset List

Need:

- tonnage bar / counter,
- hopper indicator,
- Scrap icon,
- Component icon,
- interact prompt,
- fabrication panel,
- upgrade panel,
- relic card,
- Bio-Core choice panel,
- Departure confirmation,
- temporary/permanent summary.

Avoid building a comprehensive menu framework before needed.

---

# 38. Humor Content Required

Minimum slice humor:

- PARKR-7,
- 3–5 comedic trash item names/descriptions,
- one funny Collector Bot behaviour,
- one recycler error/status message.

Possible status messages:

> RECYCLING EFFICIENCY: ACCEPTABLE.

> ORGANIC LIFE DETECTED.  
> PLEASE DO NOT RECYCLE.

> SORTING ERROR: OBJECT TOO CURSED.

Use sparingly.

The game should not become constant joke text.

---

# 39. Tutorial Philosophy

No large tutorial windows.

Teach through environment and contextual prompts.

Example:

Player approaches trash.

**HOLD [INPUT] — VACUUM**

After first hopper fills:

**RETURN TO RECYCLER**

At first tire:

**HOLD [SECONDARY] — LATCH**

Prompts disappear after successful use.

---

# 40. Player Feedback Requirements

Every meaningful action needs at least two feedback channels.

Vacuum intake:

- movement + sound.

Upgrade purchase:

- UI + tool visual or behaviour.

Machine activation:

- animation + sound.

Relic:

- VFX + audio + card.

1 tonne:

- HUD burst + Ark response + audio.

Planting:

- growth animation + biodome visual change + sound.

---

# 41. Technical Slice Architecture

Recommended scenes:

### ExteriorScene
Site A and B use different data/configuration.

### ArkInteriorScene
One room.

### UI Overlay
Recycler/fabricator and temporary upgrades.

No separate world-map scene required for slice.

Departure may transition directly to Site B.

---

# 42. Core Systems / Scripts

Engine-agnostic system list:

### PlayerController
Movement / aim.

### VacuumController
Cone query, suction forces, collection.

### Hopper
Capacity and held mass.

### TrashObject
Mass, type, reward, state.

### Recycler
Deposit conversion.

### RunUpgradeManager
Three upgrades.

### ExcavatableObject
Latch and release behaviour.

### JunkStructure
Dependency/collapse triggers.

### RelicManager
Persistent discovery.

### MachineManager
Placement and runtime.

### CollectorBotAI
Target → collect → deposit loop.

### ProgressManager
Tonnage and milestone.

### BiodomeManager
Permanent ecology choice.

### SaveManager
Persistent / expedition state separation.

### DepartureManager
Reset + scene transition.

---

# 43. Trash Implementation

Avoid full Rigidbody simulation for every visible piece.

Suggested states:

### Sleeping
No simulation.

### Attracted
Kinematic movement toward vacuum.

### Cascade
Temporary physics or scripted tumble.

### Collected
Disabled and returned to pool.

This allows large quantities of garbage without unnecessary overhead.

---

# 44. Collector Bot AI

Simple state machine:

**IDLE**

↓

**FIND NEAREST VALID TRASH**

↓

**MOVE**

↓

**COLLECT**

↓

If capacity available:
find another.

Else:

**RETURN TO RECYCLER**

↓

**DEPOSIT**

↓

repeat.

No complex navigation features beyond what the small map requires.

---

# 45. Save Data for Slice

Persistent:

- Routing Chip discovered,
- Collector Bot blueprint unlocked,
- chosen biodome upgrade,
- Site A marked recovering,
- Departure count.

Temporary:

- Scrap,
- Components,
- temporary upgrade levels,
- current hopper,
- Site A tonnage.

At Departure:

temporary state resets.

---

# 46. Telemetry / Debug Values

Even if formal analytics are not implemented, record during internal tests:

- time to first trash pickup,
- time to first full hopper,
- time to first upgrade,
- time to first excavation,
- time to relic,
- time to Collector Bot,
- time to 500 kg,
- time to 1,000 kg,
- number of recycler trips,
- player idle time,
- total trash collected by bot,
- whether player departs immediately at 1 tonne,
- chosen ecological upgrade.

This can simply be logged locally during development.

---

# 47. Playtest Questions

After a playthrough, ask testers:

### Cleaning

How satisfying was vacuuming trash?

1–5

What specifically felt best?

What specifically became repetitive?

---

### Excavation

Did pulling apart larger junk piles make cleanup more interesting?

1–5

Were you ever unsure what to do?

---

### Automation

Did the Collector Bot feel useful?

1–5

Did you enjoy seeing it work independently?

Would you want more machines like it?

---

### Progression

Did you notice yourself becoming more powerful?

1–5

Did reaching 1,000 kg feel like an achievement?

---

### Departure

Did leaving the area feel like losing progress or completing something?

Why?

---

### Biodome

Did you care about choosing what to plant?

1–5

Did the Site B payoff make your choice feel permanent?

---

### Overall

Would you voluntarily start another restoration site?

**Yes / Maybe / No**

If no, why not?

This question matters more than most others.

---

# 48. Internal Success Criteria

These are development gates, not industry benchmarks.

A promising slice should produce approximately:

### Vacuum
Most testers rate base cleaning 4/5 or better.

### Excavation
Most players understand it without extensive explanation.

### Automation
Players notice the Collector Bot contributing without being told to watch it.

### Pacing
Typical first 1 tonne falls inside ~20–25 minutes.

### Prestige clarity
Most testers correctly explain what is temporary and what persists.

### Replay desire
A clear majority say they would willingly begin another site.

If replay desire is weak, do not respond by adding more progression systems.

Fix the core loop first.

---

# 49. Warning Signs

Stop and reassess if playtests repeatedly show:

### "Vacuuming is boring."

Do not add content.

Improve interaction feel.

### "Returning to the recycler is annoying."

Increase hopper capacity, shorten routes or improve deposit flow.

### "I ignored the bot."

Improve machine visibility/usefulness.

### "Excavation is just waiting."

Make it more physical and consequential.

### "Why did my upgrades disappear?"

Improve Departure fiction and persistent payoff.

### "The biodome is just another upgrade menu."

Make growth visual and behavioural.

---

# 50. Prototype Build Order

## Phase 1 — Greybox Cleaning

Build only:

- movement,
- vacuum,
- 30 trash objects,
- collection,
- tonnage.

Do not proceed until this is enjoyable.

---

## Phase 2 — Recycler Rhythm

Add:

- hopper,
- central recycler,
- deposit,
- one upgrade.

Test:

Does returning to base improve or harm pacing?

---

## Phase 3 — Excavation

Add:

- heavy object,
- latch,
- one collapsing junk pile.

Test:

Does this create a satisfying contrast to vacuuming?

---

## Phase 4 — Automation

Add:

- Scrap economy,
- Routing Chip placeholder,
- Collector Bot.

Test:

Does automation make the world feel alive and accelerate progress?

---

## Phase 5 — Complete Site

Build:

- full 1.3t Site A,
- upgrade pacing,
- large heap,
- PARKR-7.

---

## Phase 6 — Meta Loop

Add:

- Bio-Core,
- biodome,
- two ecology choices,
- Departure,
- Site B teaser.

---

## Phase 7 — Polish

Only after the loop works:

- final art,
- animation,
- sound,
- particles,
- dialogue,
- camera polish.

---

# 51. Hard Cut Order

If development time becomes constrained, remove features in this order:

1. extra comedic trash descriptions,
2. second junk structure variation,
3. advanced NPC animation,
4. second ecological choice,
5. Site B environmental variation,
6. Collector Bot placement freedom — use fixed build pad instead.

Do **not** cut:

- vacuum juice,
- excavation,
- machine automation,
- 1 tonne milestone,
- biodome growth,
- Departure.

Those are the slice.

---

# 52. Definition of Done

The vertical slice is ready for meaningful external playtesting when a new player can, without developer intervention:

1. move,
2. vacuum trash,
3. fill and empty hopper,
4. purchase an upgrade,
5. excavate a heavy object,
6. reveal a junk cascade,
7. discover the Routing Chip,
8. build a Collector Bot,
9. understand that the bot helps automatically,
10. encounter PARKR-7,
11. reach 1,000 kg,
12. enter the biodome,
13. spend a Bio-Core,
14. see permanent ecological growth,
15. depart,
16. arrive at Site B,
17. observe the permanent ecological perk,
18. express whether they want another cycle.

The build should contain no blocker bugs through that sequence.

---

# 53. The Single Most Important Slice Moment

The defining sequence should be:

The player is vacuuming manually.

Their Collector Bot is cleaning somewhere else.

They pull a heavy object from a junk heap.

The heap collapses.

Dozens of pieces spill outward.

The player sweeps through them with their upgraded vacuum.

The 1,000 kg counter completes.

The Ark wakes up behind them.

**RESTORATION READY.**

That moment combines:

- active cleaning,
- excavation,
- automation,
- progression,
- spectacle,
- anticipation.

If that feels great, the project has something worth expanding.

---

# 54. Vertical Slice Player Promise

At the end of thirty minutes, the player should understand:

> **I arrive somewhere ruined.**

> **I personally start cleaning it.**

> **I turn its garbage into better tools.**

> **Eventually the machines and nature I unlock begin helping me.**

> **I uncover permanent pieces of the old world.**

> **I turn one tonne of waste into new life.**

> **Then I leave the place better than I found it and carry that progress somewhere new.**

If the prototype communicates that without needing an explanation from the developers, it has done its job.