# Licensed audio selection

Import from your owned archives before building/running (Python 3.9+, standard library only):

```powershell
python Scripts/import_audio.py "C:\Users\matic\Downloads"
```

The importer extracts only the exact entries in `ovani-import.json`, checks existing files by SHA-256, and refuses to overwrite modified assets. `Ovani/` is Git-ignored: the originals remain in Downloads and licensed source audio is not committed. Normal builds copy imported files beside the executable. Other checkouts/build machines must import their licensed copies first. Missing files fail with an import instruction.

## Currently wired

| Use | Source | Runtime behavior |
| --- | --- | --- |
| Main menu | Metal Vol. 3 — Fallen Angel | Low-intensity mix; two-second fade on entry |
| Session preparation | Metal Vol. 3 — Chin Surgery | Crossfade from menu, ramp to 25% intensity over four seconds |
| Hover / keyboard focus | UI & Menus — Mouse Hover Soft A | 18% cue gain, through UI → FX → Master |
| Click | UI & Menus — Click Tick | 35% cue gain, through UI → FX → Master |

Both music cues retain all three full-length alternate mixes: Intensity 1, Intensity 2, Main. They are not additive stems. Bootstrap preloads six music WAVs and two UI WAVs before menu playback. Opening settings does not create another player. The shared loading flow publishes the new in-memory session only when preparation completes. No saves/slots are created. `SessionScene` runs the robot movement/aim/fire prototype with session music; weapon sounds are not wired yet. Escape returns to the menu.

Music source PCM is stereo 24-bit / 48 kHz. Each Fallen Angel version contains 4,629,429 frames; each Chin Surgery version contains 5,246,590. Source `LIST/adtl` cue labels say **112 BPM**. Both source folders specify **RT 6.455**, used as the tail duration rather than guessing a default. Loop end is source frame count minus 309,840 tail frames. The supplier's rounded tail values and musical downbeats still need a listening pass: this is a provisional track selection, not an auditioned final soundtrack. Transitions use timed crossfades, not an assumed beat match. The engine's bar-scheduling API remains available once meter/downbeats have been verified.

These two decoded three-mix cues use about 226 MiB of PCM memory. The rest of the catalog is not decoded or copied into the build. Enemy-driven intensity/boss selection is not implemented yet.

## Supplied reserve packs

Metal Vol. 1/2, Synthwave, Heavy Electronic, Electronic Vol. 1/5, Orchestral Rock, Fantasy Vol. 2, and Science Fiction FX remain in the supplied ZIPs for later selections. Metal Vol. 3 and UI & Menus retain their unused tracks/effects in the source archives. The imported music license-link PDF is preserved under `Ovani/License`; keep purchase/subscription records separately.

```powershell
dotnet run --project Tests/Graphite.Audio.Tests -- --assets
```

This opt-in check verifies actual selected WAVs, matching intensity lengths, tail markers, cached reuse and offline crossfade output. It does not substitute for a listening review.
