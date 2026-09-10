# AFTERGREEN playtest pass

## Run without developer intervention

1. Start an expedition; sweep litter and fill the 100 kg hopper.
2. Return to the Ark, recycle and fit an upgrade.
3. Pull the Tire Anchor while moving away; vacuum the electronics cascade.
4. Pull both Relic Heap anchors; read the Routing Chip card.
5. Fabricate the Collector Bot; watch its first deposit without helping it.
6. Free PARKR-7; explore the western metal cache.
7. Reach one tonne and enter the biodome.
8. Choose a habitat and inspect the permanent/temporary departure summary.
9. Depart and use the ecological perk on Site B.
10. Close and relaunch during Site A and after departure to check resume behavior.

## Tune using local playtest.json

Compare first pickup, hopper fill, upgrade, excavation, relic, bot deployment/deposit, 500 kg, 1,000 kg and departure timestamps against the GDD. Aim for 20–25 minutes to one tonne and 10–20% bot contribution after construction. The first implementation has not yet been validated with a timed human playthrough.

Ask whether sweeping is enjoyable, pulling is understandable, the bot's contribution is visible, and departure feels like advancement. Prioritize these answers over new systems.

## Known prototype simplifications

- Procedural geometric art; no final authored sprites or animation sheets.
- Synthesized feedback sounds; no composed music or recorded material/ambient sound pass yet.
- Fixed collector deployment pad, one collector and direct steering on an open map.
- Collapses reveal scattered pieces with a particle burst rather than rigid-body debris physics.
- Biodome and departure are illustrated overlays rather than a walkable interior and fully animated Ark vehicle sequence.
- Mouse/keyboard controls; no controller aiming or vibration yet.
- Pacing is configurable and instrumented, not yet externally playtested.

Keep the existing full and vertical-slice GDDs as the design reference. The build is a playable prototype for iteration, not an assertion that every polish criterion in the GDD is complete.
