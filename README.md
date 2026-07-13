# PROJECT ETERNITY

*A real-time civilization garden. You don't command your people — you influence them,
and they answer back.*

Watch a civilization grow across 6,500 years of abstract real time. Cities found
themselves, develop personalities written by the land under them (desert towns raid,
river towns trade, mountain towns forge), drift with your laws, birth religions, riot,
secede, and — if you're careless — drag you into wars you never ordered. You steer with
policy, influence, and the crises that land on your desk.

**Full design document: [`docs/GDD.md`](docs/GDD.md)** — the shape of the game, every
system, and every design decision.

---

## Opening the project

1. Open the folder in **Unity 2022.3 LTS** (any 2022.3.x; it will prompt to use your
   installed version). Built-in render pipeline, no packages beyond default modules.
2. Open any scene (e.g. `Assets/Scenes/Main.unity`) — or none at all.
3. Press **Play**. The entire game builds itself from code at runtime: world, camera,
   UI, everything. No prefabs, no scene wiring, no art assets (colored primitives only,
   by design — art comes later).

## How to play

- **First click matters most:** you begin by choosing your homeland. Inspect tiles
  (click), read the site quality, then *Found Your Capital*. The geography you pick
  writes your people's first draft.
- **Top bar:** year/era, gold, research, influence, speed controls.
- **Left tabs:** Empire (law sliders, spending, stance, research focus) · Faith
  (adopt/suppress/tolerate) · Diplomacy (war, peace, gifts) · Victory (progress).
- **Right panel:** selected city — its personality, districts, faiths, and *why* its
  people feel the way they do. Player cities offer acts of the throne: festivals,
  investment, settlers, raising armies, restraining raiders.
- **Bottom:** the Chronicle. History narrates itself; click an entry to jump the camera.
- **Decisions** arrive as modal dilemmas (plagues, famines, prophets, ultimatums).
  Ignore them and they resolve themselves — badly, and your legitimacy pays.

### Controls

| Input | Action |
|---|---|
| WASD / arrows | pan camera |
| Q / E | rotate |
| Mouse wheel | zoom |
| Middle-drag | pan |
| Left click | select tile / city |
| Space | pause / resume |
| 1 2 3 4 | speed 1× 2× 4× 8× |
| Esc | deselect / cancel targeting |

### Ways to win

**Dominion** (60% of humanity) · **Ascension** (finish the final project) ·
**Harmony** (a long age of genuine contentment) · **Legacy** (highest score when the
Long Count ends in 2500 AD). Lose your last city and your story ends.

## Project layout

```
Assets/Scripts/
  Core/   Bootstrap, GameController (clock), Tuning (ALL balance constants), Rng, names, chronicle
  Sim/    the whole living world — pure C#, no scene dependencies
  View/   primitives-only rendering: terrain boxes, city cylinders, marching armies
  UI/     immediate-mode interface (code-only)
docs/GDD.md          the design document
tools/balance-sim/   headless eon-runner for balance work (no Unity needed)
```

## Balance & tuning status

Every gameplay constant lives in `Assets/Scripts/Core/Tuning.cs`, grouped and
annotated. The current values were fitted empirically with the headless harness in
`tools/balance-sim/` (whole worlds run start-to-finish in seconds): era pacing is
plausible, wars actually happen, victory types vary by seed, and a fully passive
player survives about two worlds in three. It is a first fit, not a final balance —
that pass is deliberately left open.
