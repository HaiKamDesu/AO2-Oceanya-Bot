# Oceanya Laboratories Client

### *a.k.a. The Multi-Attorney-Online Client*
(Last version: 1.0.4)

Welcome to the chaos-slaying, GM-saving, INI-wrangling tool you never knew you needed but now can’t live without: **The Oceanya Client (tm)**.

## What is this?

This lovely abomination of a program is built for **Attorney Online** GMs who are tired of juggling seven trillion clients, only to lose track of which INI said what, where, and why. So, we made this. One window to rule them all. Multiple clients, same screen, no alt-tab hell.

In short? It’s an **AO2-compatible multi-client manager** that lets you run several AO clients inside a single app, each one easily accessible, controllable, and nameable. Yes, you can name your clients. Yes, you can make them talk with a command like `Edgeworth: Put a shirt on.`

## Core Features

- **Multiple Clients:** Add as many as your RAM can handle. Hit "Connect Client" and boom—another client. Toss 'em in, toss 'em out.
- **IC/OOC Quickswap:** Press TAB while typing in IC/OOC to instantly swap textboxes. No more clicking around.
- **Focus QoL:** Various items have been modified to not lose focus of IC textbox, meaning it's easier to type than ever.
- **Internet Disconnect Protection:** If your internet flakes, clients auto-reconnect like champs. ~~Mods can’t kick what never leaves.~~
- **INI Dropdown Overload:** Actually shows *all* of your INIs. With icons. Yes, those icons you forgot existed.
- **Keybinds:** CTRL + Number to select clients. Because mouse movement is for the weak.
- **Dialogue Command:** Type `NameOfClient: (text)` and that client will say it. Magic.

## Based On AO2

Oceanya Client mimics AO2’s layout and features as closely as possible, so it’s intuitive if you’re coming from regular Attorney Online. This includes:

- INI handling
- Emotes
- IC/OOC chat
- SFX & color dropdowns
- Effects & status toggles
- INI selection menus

BUT—and this is a big but—it’s still a solo dev project, so some features from AO2 didn’t make the cut (yet):

- Pairing
- Area viewing (you *can* still use `/area`)
- Y/X offset
- Judge controls
- Evidence
- Viewport (no emote/effect visuals for now)

Think of this more like a **GM console**, not a full visual client. If you need visuals, run a regular AO2 client on the side.

Currently, Oceanya Client only works with **Chill and Dices** server, because that’s where the dev's RPG group lives. Multi-server support *might* happen someday.

## Nerd Stuff / QoL Things

- Super-optimized asset refreshing after the first load.
- INI auto-refreshes when you swap to it, reflecting changes in character folders.
- SFX dropdown: Basic, but there.
- AltGr key won’t trigger weird keybinds anymore.
- Logs are configurable in `config.ini`.

## Final Notes

This is a ground-up project. That means:

- Everything can be added.
- Everything can be broken.
- Maybe one day selecting Edgeworth launches DOOM. Who knows.

If you find differences between Oceanya and AO2 in terms of **base behavior**, that’s probably unintentional. Let me know.

Got feature requests? Bug reports? Life advice? Toss 'em in. I might even do something about it maybe. Scorpio2#3602 on Discord.
