# RITES 🪔
### *Asymmetrical PvP — 3v1 Multiplayer Horror Experience*

> **Worship or Haunt?** — 4 Players | South Indian Temple Setting | PC Multiplayer

---

## Table of Contents

- [Overview](#overview)
- [Game Pillars](#game-pillars)
- [Core Loop](#core-loop)
- [Entities](#entities)
  - [The Priests](#the-priests)
  - [Vamika — The Ghost](#vamika--the-ghost)
- [World](#world)
- [Mechanics](#mechanics)
  - [Ritual Meter](#ritual-meter)
  - [Ritual Tasks](#ritual-tasks)
  - [Traps](#traps)
  - [Collectibles](#collectibles)
  - [Combat](#combat)
- [Map & Level Design](#map--level-design)
- [Systems & UI](#systems--ui)
- [Audio](#audio)
- [Monetisation](#monetisation)
- [Team](#team)
- [Resources & Tools](#resources--tools)

---

## Overview

**Rites** is a 3 vs 1 asymmetrical PvP multiplayer game combining combat and ritual-based objectives inside a haunted South Indian temple dungeon. Three **Priests** enter the temple to complete the final rites of **Vamika**, a ghost who hunts them down. The game is a constant tug-of-war — priests manipulate the environment to advance the ritual, while the ghost disrupts and destroys their progress.

| | |
|---|---|
| **Genre** | Asymmetrical PvP / Multiplayer Horror |
| **Players** | 4 (3 Priests vs 1 Ghost) |
| **Platform** | PC (Flat Screen) |
| **Engine** | Unity 6 |
| **Controls** | Keyboard + Mouse, Proximity Voice Chat |

**Priest Objective:** Complete 5 Ritual Tasks  
**Ghost Objective:** Kill all 3 Priests simultaneously

---

## Game Pillars

| Pillar | Description |
|--------|-------------|
| 🔥 **Asymmetric Excitement** | The thrill of 3 players coordinating against a single, powerful enemy — or 1 player outmaneuvering a team |
| 🏛️ **Authenticity** | An authentic experience rooted in ancient South Indian temple architecture, culture, and spiritual traditions |
| ⚡ **Tension & Replayability** | Constant pressure encourages diverse player strategies and emergent moment-to-moment gameplay |

---

## Core Loop

```
Priests collect flowers → Light oil lamps → Fill the Ritual Meter
       ↓                                            ↓
Ghost extinguishes lamps                  Gates to Ritual Area open
Ghost deploys traps & souls         Priests complete 5 Ritual Tasks → WIN
       ↓
Ghost kills all 3 priests simultaneously → WIN
```

**Progression is match-based, not level-based.** There are no XP or rank gates — victory comes from controlling objectives and outlasting the opposing side.

---

## Entities

### The Priests

Three cooperative players whose primary goal is to perform the sacred rites to liberate Vamika's spirit.

**Responsibilities:**
- Protect and relight sacred oil lamps (diyas) across the temple
- Collect flowers for ritual offerings
- Complete 5 ritual tasks inside the central Ritual Area
- Revive fallen teammates by returning their urn to the spawn point

**Design Inspiration:** Visually grounded in South Indian *Kalaripayattu* martial arts — physical discipline, ritual authority, and cultural authenticity.

**Stats:**

| Element | Value |
|---------|-------|
| Health | 100 |
| Projectile Damage | 10 |
| Projectile Fire Cooldown | 0.2 sec |
| Projectile Count | 10 |
| Projectile Cooldown Duration | 3 sec |
| Projectile Range | 28 |
| Shield Duration | 6 sec |
| Shield Cooldown | 6 sec |
| Respawn | Requires Teammate Revival |

**Abilities:**
- 🛡️ **Shield** — Right-click to absorb incoming damage for 6 seconds (6s cooldown)

---

### Vamika — The Ghost

The single antagonist player. A tragic spirit bound to the temple she once served — a South Indian temple dancer, not a generic horror entity.

**Responsibilities:**
- Disrupt sacred worship items to force priests away from ritual tasks
- Place traps across high-traffic areas
- Collect and deploy souls to extinguish lamps remotely
- Hunt and eliminate all three priests simultaneously

**Design:** Luminous blue skin reflecting a cursed spiritual state; red attire and gold ornaments preserving cultural identity. Gameplay emphasises agility, stealth, and environmental control.

**Stats:**

| Element | Value |
|---------|-------|
| Health | 100 |
| Projectile Damage | 20 |
| Projectile Fire Cooldown | 0.2 sec |
| Projectile Count | 15 |
| Projectile Cooldown Duration | 3 sec |
| Projectile Range | 40 |
| Respawn Time | 12 sec (automatic) |

**Abilities:**
- 🌿 **Vine Trap** — Immobilises a priest for 4 seconds (3 available per match)
- 🩸 **Blood Pool Trap** — Deals 5 damage/sec to priests standing inside (3 available per match)
- 👻 **Soul Deployment** — Release collected souls to remotely extinguish lamps

---

## World

### Setting

The game takes place entirely within the **basement dungeon of an ancient South Indian temple**, drawing architectural inspiration from real structures like the **Srirangam Temple** and **Kapaleeshwarar Temple**.

**Visual Identity:**
- Carved brick stone walls and stone-tiled floors
- Long, thick stone pillars with intricate carvings
- Fire as the primary light source — torches and orange area lights casting warm, flickering amber glow
- Colour palette: deep stone greys and browns warmed by firelight — sitting between *sacred* and *sinister*

### Map Layout

The map is **150m × 150m** — a single continuous looping dungeon with no doors, dead ends, or shortcuts. All areas share the same flow of play.

| Area | Count | Description |
|------|-------|-------------|
| **Priest Spawn Locations** | ×3 | Three separate spawn rooms at different ends of the map; also the revival point for dead priests |
| **Ghost Spawn Location** | ×1 | Located at the fourth end of the map |
| **Collectible Rooms** | ×9 | Open rooms with flowers (priests) and souls (ghost); natural conflict zones with hiding spots |
| **Open Corridors & Halls** | — | All 15 sacred diyas are placed here; accessible to both factions at all times |
| **The Ritual Area (Centre)** | ×1 | Primary priest objective; ghost **cannot enter**; unlocks when 10/15 diyas are lit |

### Ritual Area

Inside the central ritual area a **havan** (sacred fire pit) sits at the centre with five ritual tasks arranged around it:
- 🔔 Three bells
- 🌸 A flower offering shrine
- 🐚 A conch on a pedestal
- Two tasks are repeats from the above three

---

## Mechanics

### Ritual Meter

The Ritual Meter is the central resource governing the entire match. It is directly tied to the number of **lit oil lamps** out of 15 placed across the map.

| Ritual Health State | Condition | Effect |
|--------------------|-----------|--------|
| **Ritual Health 2** | Meter above 2/3 | Tasks spawn and progress; priests are protected inside the Ritual Circle |
| **Ritual Health 1** | Meter between 1/3 and 2/3 | Tasks pause; priests **lose protection** inside the circle; ghost can attack anywhere |
| **Ritual Health 0** | Meter below 1/3 | Same as Health 1 + Ghost enters **Frenzy Mode** (bonus speed & attack speed) |

### Ritual Tasks

Tasks spawn inside the Ritual Area once the meter exceeds 2/3. Five tasks must be completed to win.

| Task | Action | Count |
|------|--------|-------|
| **Ring the Bells** | Walk up and press interact at all 3 bells | Fixed — 3 bells |
| **Flower Offering** | Button mash at the offering shrine (requires flowers) | Random — 6 to 13 inputs |
| **Play the Conch** | Hold interact button at the conch pedestal | Fixed — 4 seconds |

Any single priest can complete any task alone. Tasks repeat across the required 5 completions. There is a short wait period between task spawns.

### Traps

The ghost has a fixed pool of traps per match — once placed, they **cannot be destroyed** and remain active for the entire match.

| Trap | Effect | Available |
|------|--------|-----------|
| 🌿 **Vine Trap** | Immobilises a priest for 4 seconds | 3 per match |
| 🩸 **Blood Pool Trap** | Deals 5 damage/sec continuously | 3 per match |

### Collectibles

**Flowers (Priests)**
- Found at up to 10 locations across the map
- Spots regenerate automatically over time
- Stored as a shared count across all three priests
- Required for the Flower Offering ritual task

**Souls (Ghost)**
- Collected from fixed locations with no carrying limit
- Each released soul autonomously travels to the nearest lit lamp and extinguishes it, then dissipates
- Killing a priest also rewards the ghost with souls

### Combat

Both factions use a **projectile attack** aimed with a crosshair. The ghost deals double the damage per shot and has greater range and ammo — priests must rely on teamwork and positioning, not individual combat.

**Priest Revival System:**
- When a priest dies, they drop an **urn** at the location of death
- A surviving teammate must pick up the urn and carry it to the fallen priest's spawn point to revive them
- If the carrying priest is killed, they drop **both** urns simultaneously
- The ghost must kill **all 3 priests at the same time** to win — a single surviving priest keeps the others in contention

### Win & Lose Conditions

| Side | Victory Condition |
|------|-------------------|
| **Priests** | Complete all 5 Ritual Tasks inside the Ritual Area |
| **Ghost** | Kill all 3 priests **simultaneously** before the ritual is complete |

There is **no time limit** on a match.

---

## Map & Level Design

### Gameplay Beat Chart

| Beat | Phase | Dominant Feel |
|------|-------|---------------|
| 1. Cinematic | Main menu opens | Anticipation |
| 2. Spawn | Roles assigned, players spawn | Orientation |
| 3. Early Exploration | Collect flowers, begin lighting diyas | Calm, strategic |
| 4. Lamp Struggle | Race to light 10/15 diyas, avoid traps | Tense, cautious |
| 5. Regrouping | Coordinate via proximity voice chat | Tense, dangerous |
| 6. Gates Open | 10 diyas lit; priests enter Ritual Area | Escalating intensity |
| 7. Ritual vs Disruption | Perform tasks while managing lamp count | Chaotic, high stakes |
| 8. Combat & Revival | Fight, relight lamps, retrieve urns | Desperate, intense |
| 9. Resolution | Victory or defeat | Relief or defeat |

### Numerical Reference

| Element | Value |
|---------|-------|
| Total oil lamps on map | 15 |
| Lamps needed to unlock Ritual Area | 10 |
| Ritual tasks required to win (priests) | 5 |
| Ritual task types | 3 |
| Ghost vine traps per match | 6 |
| Ghost blood pool traps per match | 6 |
| Flower spawn locations | 10 |
| Priests required dead simultaneously (ghost wins) | 3 |
| Soul spawn locations | 10 |

---

## Systems & UI

### Sequence of Play

```
Main Menu → Login / Guest → Lobby → Room Creation → All 4 Players Join
→ Room Creator Starts Match → Opening Cinematic → Roles Randomly Assigned
→ Match Begins → Win Condition Met → Victory / Defeat Screen → Return to Lobby
```

### Roles

Roles are assigned **randomly** at the start of each match. Players cannot select their faction. The assigned role is displayed in the top-right corner of the screen throughout the match.

### HUD Elements

**All Players:**
- Health bar
- Crosshair
- Ability icon row with keybindings
- Ritual meter (15 diya tracker)
- Task meter & task timer
- Role indicator (top-right)
- Objective notification panel

**Priests additionally:**
- 🌸 Flower count icon
- 🛡️ Shield icon

**Ghost additionally:**
- 👻 Soul count
- 🌿 Vine trap count
- 🩸 Blood pool trap count

### Controls

| Action | Key |
|--------|-----|
| Movement | W A S D |
| Shoot / Attack | LMB |
| Shield (Priest) | RMB |
| Interact | E |
| Vine Trap (Ghost) | X |
| Blood Pool Trap (Ghost) | C |
| Souls (Ghost) | G |
| Camera | Mouse |

### End Screens

| Outcome | Priests Screen | Ghost Screen |
|---------|---------------|--------------|
| Priests complete 5 tasks | **VICTORY** — Ritual Complete. The Lamp is Lit. | **BANISHED** — The Spirit Fades. The Ritual is Done. |
| Ghost kills all 3 priests | **DEFEAT** — The Ritual Failed. Darkness Remains. | **HAUNTED** — The Spirit Prevails. The Veil Holds. |

---

## Audio

### Approach

Audio is designed to feel **sacred and oppressive simultaneously** — as though the temple itself is alive and hostile. The underscore is static by design, placing the burden of tension entirely on the players rather than reactive music cues.

### Audio Cues

| Event | Audio Cue |
|-------|-----------|
| Lamp lit by priest | Audio feedback sound |
| Lamp extinguished by ghost or soul | Audio feedback sound |
| Ritual meter crosses 2/3 threshold | Distinct audio sting |
| Bell ringing task | Bell sounds |
| Conch playing task | Conch sound |
| Flower offering task | Small bell ringing |
| Vine trap triggered | Trap trigger sound |
| Blood pool trap triggered | Trap trigger sound |
| Priests win | Victory sound (priests) / Defeat sound (ghost) |
| Ghost wins | Victory sound (ghost) / Defeat sound (priests) |

### Proximity Voice Chat

All 4 players share a **proximity-based voice chat** — players hear each other based on in-world distance. Priests can be overheard by the ghost, and vice versa. **Communication itself becomes a vulnerability.**

### Intro Cinematic

A narrated opening cinematic voiced from **Vamika's perspective**, establishing her identity, connection to the temple, and hostility toward the priests. Skippable.

---

## Monetisation

The game is **free to play** with no upfront cost. All paid content is cosmetic — no purchases affect gameplay balance.

| Model | Details |
|-------|---------|
| 🎫 **Battle Pass** | Seasonal, purchased with proxy currency; contains character skins, wall decor, animations, music tracks |
| 📦 **DLC** | Quarterly release; new ghost character with unique story, map, cultural setting, and gameplay abilities |
| 🛒 **Direct Cosmetics** | Available in the in-game shop at any time |

### DLC Roadmap

Each DLC expands the world geographically across India (south to north), introducing a new ghost, new priests, and a culturally distinct map. Base game features **Vamika** in a South Indian temple. Future releases (e.g. **Rhea**) introduce new settings, lore, and abilities.

### Live Ops

- Quarterly DLC + battle pass cycle
- Free ability updates between DLC releases (e.g. invisibility, petrify, freeze mechanics)
- Profile achievements tied to in-game milestones (e.g. *"Win 5 games in a row"*, *"Play 5 times as the ghost"*)

---

## Team

**Course:** MA Game Design & Development — Kingston University London

| Name | Role | ID |
|------|------|----|
| Abhilasha | Game Designer / Gameplay & System Designer | K2462789 |
| Mohammed Kouzer | Game Designer / Level Designer | K2534059 |
| Merlin Agnes Dsouza Kumar | Game Design / 2D & 3D Artist | K2542593 |
| Zeeshan Ahmed | Game Programmer | K2511209 |

---

## Resources & Tools

| Tool | Purpose |
|------|---------|
| [Unity 6](https://unity.com/) | Game Engine |
| [Maya](https://www.autodesk.com/uk/products/maya/overview) | 3D Modelling |
| Krita | Concept Art & Digital Painting |
| Substance Painter | Texturing |
| Capcut | Audio/Video Editing |
| ElevenLabs | Voice Generation |
| Canva | Presentation |
| YouTube / Pixabay | Audio Assets |
| Sketchfab / CgTrader / BlenderKit / Unity Asset Store | Models & VFX |
| Pinterest | Concept Art References |
| ChatGPT / Claude / Gemini | Image References |

---

*Rites — Where the sacred becomes sinister.* 🕯️
