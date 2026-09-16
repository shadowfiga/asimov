# Engine audio

`AudioManager.Current` is the game-thread, engine-wide audio service. `GameHost` restores its preferences before the first scene, updates it independently of UI, and disposes it after scene/UI teardown. No sound assets are assigned to the menu yet.

## Mixer and settings

| Bus | Parent | Preference | Pauses with the game |
| --- | --- | --- | --- |
| Master | — | `audio.masterVolume` | — |
| Music | Master | `audio.musicVolume` | Yes |
| FX | Master | `audio.fxVolume` | Yes |
| Ambience | Master | `audio.ambienceVolume` | Yes |
| UI | FX | `audio.uiVolume` | No |

Use the typed keys in `AudioPreferences`; the existing three `PlayerPreferences` keys forward to them without changing saved key names. All five sliders are exposed in Settings → Audio. Levels default to 1 and range from 0 to 1; zero silences a channel without stopping its playback clock. There is no additional mute setting. The old `audio.muted` preference migrates to zero Master once.

Effective volume is **voice gain × spatial attenuation × bus/ancestor user volumes × bus/ancestor snapshot gains**. Music goes through Music, never FX. UI remains part of FX but has its own sub-level. Ambience is independently adjustable. For another category, call `CreateBus("dialogue", audio.Master)` and route voices to that bus. Custom bus preferences/UI are explicitly authored rather than automatically adding settings.

Setting a bus volume changes active voices immediately. Changes to persisted preferences are also picked up by the host's next audio update, without an open settings dialog. Temporary snapshots/ducking do not modify user preferences. Do not set `SoundEffect.MasterVolume`, use `MediaPlayer`, or create unmanaged sound instances for game playback: those bypass this routing.

## 2D positional playback

```csharp
var audio = AudioManager.Current;
// During loading; paths are relative to the executable, or absolute.
var drillClip = audio.PreloadEffect("Content/Audio/drill.wav", spatial: true);

// In a Scene: Audio is its automatically disposed AudioScope.
var drill = Audio.Play2D(drillClip, drillPosition,
    new SpatialAudioSettings
    {
        InnerRadius = 64,
        OuterRadius = 1000,
        PanDistance = 400,
        Rolloff = 1
    },
    new AudioPlayOptions { Loop = true, Volume = .7f });

// Update from world/camera or player coordinates, not screen/UI pixels.
audio.Listener.Position = playerPosition;
audio.Listener.Right = Vector2.UnitX;
drill.Position = drillPosition;
```

The owner chooses whether the listener follows the player or camera. Distance controls attenuation; displacement along `Listener.Right` controls stereo pan. Inner radius is full volume, outer radius is silent, and rolloff controls the curve between them. Resolution and UI scale cannot affect either calculation. Spatial effects are downmixed to mono before panning; non-spatial effects and music retain stereo.

Use `Play` for non-positional UI, global warnings or ambience beds; use `Play2D` for machinery, weapons and enemies. Keep a looping emitter's handle and update its position, rather than restarting the clip each frame. `Pause`/`Resume` on a voice are independent of manager-level pause. A scene's `AudioScope` stops its owned voices on unload; manager-level music can continue through scene transitions. Explicitly call `AudioManager.Current.Pause()`/`Resume()` from the gameplay pause owner—this engine foundation does not implement a gameplay pause screen.

The default budget is 64 SFX/UI/ambience voices plus one music stream. At the limit, a new voice replaces the oldest lowest-priority voice only when its priority is at least as high; otherwise the returned handle is already stopped. UI cues use priority 100. Inaudible loops keep their phase and still consume a voice; do not create a permanent loop for every offscreen entity. Completed/stopped voices release their native instances, while cached assets remain until manager disposal.

## Ambience zones

`AmbienceManager2D` uses world-space rectangular areas and deterministic priority, independent of physics or scene hierarchy. Higher priority wins overlaps; registration order breaks ties. Give inner zones a higher priority than their containing zones. Leaving an inner zone restores the outer zone; leaving all zones restores unity snapshot gains.

```csharp
var audio = AudioManager.Current;
var exterior = audio.CreateBus("exterior", audio.Ambience);
var interior = audio.CreateBus("interior", audio.Ambience);
// Play the respective ambience beds on these buses.
Audio.Own(audio.AmbienceZones.Add(new AmbienceZone2D
{
    Center = worldCenter,
    HalfSize = worldHalfSize,
    Priority = 0,
    Snapshot = new AudioSnapshot(new Dictionary<AudioBus, float>
    {
        [interior] = 0
    })
}));

Audio.Own(audio.AmbienceZones.Add(new AmbienceZone2D
{
    Center = roomCenter,
    HalfSize = roomHalfSize,
    Priority = 10,
    TransitionSeconds = 1,
    Snapshot = new AudioSnapshot(new Dictionary<AudioBus, float>
    {
        [exterior] = .2f,
        [interior] = 1
    })
}));
```

Snapshots replace the complete temporary mix; omitted buses return to unity. All affected gains fade together, and interrupted fades start from the current values. For a persistent outdoor/default mix, register a lower-priority containing zone with its own snapshot rather than relying on a one-off `TransitionTo`. `Audio.Own` unregisters zones with their scene. For entity lifetimes use a disposable `AudioScope` or dispose the registration/voice directly.

Zone snapshots currently control **gain only**. There is no reverb/filter DSP graph, geometry occlusion, Doppler, or automatic assignment of moving sounds to zone buses. A moving source updates its 2D position; its bus remains explicit. This is a 2D engine implementation, not a drop-in Godot plugin port.

## Adaptive music

```csharp
var audio = AudioManager.Current;
var combat = new MusicCue("combat",
    [audio.LoadClip("Content/Audio/combat-low.wav"),
     audio.LoadClip("Content/Audio/combat-medium.wav"),
     audio.LoadClip("Content/Audio/combat-high.wav")],
    beatsPerMinute: 120, beatsPerBar: 4);

audio.MusicPlayer.Play(combat, MusicTransition.Immediate);
audio.MusicPlayer.SetIntensity(.8f, rampSeconds: 2);
// When an encounter director selects an already-loaded boss cue:
audio.MusicPlayer.Play(bossCue, MusicTransition.NextBar, fadeSeconds: 1);
```

Tempo in this example is illustrative, not verified Ovani metadata. Author actual BPM, meter, first downbeat and loop boundaries after checking the licensed audio. `MusicCue` requires matching sample rates and frame counts across its alternate intensity mixes. Their musical alignment must also be checked by ear; matching lengths alone is not enough. These are alternate full mixes, not additive instrument stems.

The music renderer blends adjacent intensity versions into **one 48 kHz stereo PCM stream**, using one frame counter for all variants. Ramps and transitions are evaluated per sample, not from elapsed game frames. Pausing freezes transport and preserves queued audio. Muting the Music bus leaves transport running. This prevents separate intensity players drifting or repeatedly restarting the composition.

* `Immediate` starts a cue from its beginning, including an authored intro.
* `NextBar` waits for the outgoing cue's next bar and aligns the incoming cue's first downbeat there. It does not beat-match incompatible tempos or keys. Use an authored transition/fade when cues are not compatible.
* `LoopStartFrame` and `LoopEndFrame` are exclusive-end source-frame markers. Audio after loop end is an optional reverb tail that overlaps the next loop. Its length cannot exceed the loop length. There is no guessed tail duration.
* Repeated requests replace the pending transition. Requesting the current cue cancels a pending switch. A new transition waits for an existing crossfade to finish, bounding playback to two cue sets.
* `Stop(0)` clears output immediately; positive stop duration fades. Music cues are currently looping; short non-looping stingers use `Play` on the Music bus.

The output queues three 1024-frame buffers (about 64 ms). `RenderedFrames` reports the rendering horizon, not the exact speaker position. The host must keep pumping audio; a main-thread stall longer than the buffered audio can underrun. `UnderrunCount` exposes this. Sample alignment is preserved on recovery, but uninterrupted playback through arbitrary loading stalls is **not** guaranteed. Load/decode assets before playback; a dedicated streaming backend remains a future improvement.

The game-side enemy/threat director, selected soundtrack, and authored transition metadata are still pending. The engine does not depend on enemy classes, UI screens, or Chisel.

## Asset formats and limits

The loader reads mono/stereo RIFF WAV: integer PCM 8/16/24/32-bit and IEEE float32, including extensible format headers, extra chunks and odd-byte padding. The native SFX backend converts to PCM16 and low-pass downsamples rates above 48 kHz during preparation. Music is mixed to PCM16 at 48 kHz; prefer already-mastered 48 kHz files for music. Resampling in the music renderer is linear, not a mastering-quality conversion. Compressed codecs, surround WAV and disk streaming are not implemented.

Decoded music is held in memory: three 90-second stereo float mixes at 48 kHz use roughly 104 MB before output/caching overhead. Preload only the selected cue set, not the whole catalog. `LoadClip` decodes/cache-loads; `PreloadEffect` also uploads native SFX buffers. Imported source files and plugin demonstration assets have not been added to the repository.

## Supplied plugin analysis

Inspected `UnityAudioPlugin1.2.zip` → `SimpleAudioManager1.2.unitypackage` → `Manager.cs` / `Song.cs`, and the GDScript sources in `EnvironmentalAmbiencePlugin_Godot_v0.6.1.zip`.

| Reference | What it does | Engine adaptation |
| --- | --- | --- |
| Unity music manager | Keeps alternate intensity clips playing, blends neighboring levels, crossfades songs, overlaps loops around an authored reverb tail. Uses `Time.time`/`Time.deltaTime`, sequential audio-source starts and frame updates. | Single PCM timeline, continuous intensity, explicit loop/tail markers, bar scheduling and bounded transitions. |
| Godot ambience plugin | Listener-triggered 3D zones select bus snapshots; snapshots include volume and effect parameters. Parent/indirect zones provide fallback. Moving-source trigger areas change routing. | 2D rectangular zones, deterministic priority, parallel gain fades, explicit bus ownership and disposable registrations. DSP effects/editor tooling are not ported. |

The Unity implementation is not a BPM-grid scheduler: its public play options contain intensity fields that `PlaySong(options)` does not apply, and its default loop tail falls back to a fixed quarter-second. The Godot implementation relies on `AudioStreamPlayer3D` for spatial sound and uses a shared tween for snapshots. Neither plugin can simply be dropped into MonoGame. The engine code is an original implementation; no Unity/Godot runtime dependency or vendor source/assets are embedded.

MonoGame backend references: [SoundEffect](https://docs.monogame.net/api/Microsoft.Xna.Framework.Audio.SoundEffect.html), [DynamicSoundEffectInstance](https://docs.monogame.net/api/Microsoft.Xna.Framework.Audio.DynamicSoundEffectInstance.html).

## Verification

```powershell
dotnet build Graphite.csproj
dotnet run --project Tests/Graphite.Audio.Tests
dotnet run --project Tests/Graphite.Audio.Tests -- --native
```

The first test command needs no audio device. The optional native check exercises MonoGame/OpenAL with a silent fixture: native effects, high sample rates, stream refill, pause/resume and stop. Tests cover routing, preferences, UI delegation, spatial math, budgets, snapshots/zones, WAV decoding, music loops/tails, downbeat scheduling, crossfades and chunk-size-independent synchronization. The older UI test project currently has unrelated compile failures from removed session play-time members.
