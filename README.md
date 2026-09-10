# AFTERGREEN

A native, top-down restoration game prototype built with the existing Graphite / MonoGame DesktopGL engine. The startup scene now runs the AFTERGREEN vertical slice.

## Play

On macOS, double-click `Play AFTERGREEN.command`. You can also run `./run.sh` (macOS/Linux) or `run.cmd` (Windows). For first-time installation, use `./setup.sh` or `setup.cmd`.

- **Enter** begins or resumes the expedition.
- **WASD / arrows** move; **mouse** aims.
- Hold **left mouse / Space** to vacuum loose debris.
- Aim at a buried object marked with a gold dot; hold **right mouse / Left Shift** and **move away** to pull it free.
- Press **E** near the Ark to recycle; press E with an empty hopper to open its workshop.
- Workshop: **1–3** buy upgrades, **4** builds the Collector Bot, **B** enters the biodome after one tonne.
- **Esc** closes panels or pauses. **M** toggles sound.
- All workshop and ecology choices also have clickable buttons.

The pause menu offers a new expedition with a second-click confirmation before replacing progress.

## The playable loop

Site A contains 1,300 kg: 220 kg loose litter, 240 kg medium debris, 270 kg buried heavy objects, and six 95 kg heaps. The Ark is central and the survey map shows remaining litter, the player and the collector.

Recycle to earn Scrap and Components. Fit the wider intake, bigger hopper and stronger motor. Excavating the Tire Anchor releases electronics. Pull both anchors from the southeastern Relic Heap to discover the permanent Municipal Routing Chip. It unlocks a Collector Bot built on the Ark's deployment pad. The bot independently finds debris and brings it home. Free PARKR-7 at the northeastern vehicle heap.

At 1,000 recycled kg, the Ark produces a Bio-Core. Enter the biodome and choose Paper Finch Habitat or Scrub Grass. Confirm departure after reviewing what stays and what travels. Site B demonstrates the chosen perk: finches physically gather paper, or grass improves collection speed. Collect 18 kg to see the slice completion screen.

## Saving and tuning

Progress autosaves every ten seconds, on selected transitions, on pause and on normal close. The save and local `playtest.json` are stored in `Environment.SpecialFolder.LocalApplicationData/Aftergreen` (normally `~/.local/share/Aftergreen` on macOS/Linux). Saves separate permanent discoveries/ecology from temporary field equipment and currency. Departure leaves field upgrades and the Collector Bot behind and preserves the relic and habitat.

Edit `Content/slice.json` to tune movement, suction, mass target, hopper capacities, costs, bot speed/capacity and trash counts. `settings.json` controls the window. No content compiler is needed: the presentation uses native rendered shapes, Myra's bundled font and synthesized sounds.

## Verification

```sh
dotnet build --no-restore --warnaserror
dotnet run -- --self-test
dotnet format Graphite.csproj --verify-no-changes --no-restore --severity warn
```

To capture all screens using an isolated test expedition, run `dotnet run -- --render-check /tmp/aftergreen-screens`. This requires native graphics access and never reads or writes your save.

The headless gameplay checks cover suction, capacity, recycling, upgrade gates, physical excavation, cascades, relic/NPC discovery, autonomous bot delivery, Bio-Core gating, ecology purchase, save/load and departure reset. They do not require a graphics device.

This is a first playable implementation, with procedural placeholder art and synthesized audio. The GDD's 20–30 minute pacing and enjoyment criteria require human playtesting; the build does not claim those gates are met. See `docs/NEXT_STEPS.md` for the playtest checklist and remaining polish.

## Code

- `Game/Aftergreen/SliceState.cs`: world, vacuum, excavation, recycler, economy, bot AI, ecology and persistence.
- `Game/Aftergreen/SliceRenderer.cs`: top-down world, particles, HUD and menus.
- `Game/Aftergreen/SliceAudio.cs`: native synthesized feedback.
- `Game/Scenes/AftergreenScene.cs`: input and engine integration.
- `Game/Aftergreen/SliceSelfTest.cs`: executable gameplay regression checks.

The earlier Black Company prototype and its Chisel-generated tables remain in the repository but are not loaded by AFTERGREEN. Engine dependencies remain MonoGame DesktopGL 3.8.5.1, Myra 1.6.5 and .NET 8 (compatible newer runtime supported).
