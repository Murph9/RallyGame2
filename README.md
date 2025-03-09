# RallyGame2 Readme

## Project setup

Download godot, setup the .vscode launch.json script to point to its location

## How TOs

### Adding a model with jolt physics

1. make model thats ready to import into godot
1. append `-col` to the model name
1. In godot import as scene, it has static model physics

### Adding a model with physics and a custom collision model

1. make model thats ready to import into godot
1. Duplicate it with shift-D (a copy non-instanced)
1. Change name to `xxxx-colonly`
1. add a decimate modifier to simplify the col mesh with properties:
    - planar with angle limit of 0.5d
    - and apply it
1. In godot import as scene, it has static model physics

---

## 100km Distance Game Idea:

An infinite world

-   spawn next to the road with a goal of driving 100km
    -   at some fixed amount 10km you will have a goal to reach:
        -   a speed trap target
        -   an average speed section
        -   time trial section
        -   ???
    -   or start with a bad car but slowly have a minimum speed you need to hit
-   come across cars to race along the road for $
    -   match their speed and challenge them
        -   win and get a random part (with rarity)
        -   if you lose you get nothing
    -   also get money per race
-   traffic
-   get money from making it distance or find along road
-   shops spawn along route
-   nos doesn't come back by default
-   tyre wear
-   fuel use
-   body damage from collisions (which affects aero and performance somehow)
-   a top speed/accel/handling estimate performance numbers
-   relic that upgrades the road type
    -   you know how peglin has 3 phases? why don't we have upgraded road types
    -   or road types are a part of the challenge, pick directions and it may slowly get there

Ideas for relics:

-   relics get unlocked by having certain parts (and maybe not others?)

-   nos refils while idling (no accel/braking)
-   nos refills based on speed
-   money for continually accelerating
-   reduced damage from collisions
-   money for near misses
-   a big helpful fan
-   hopping ability
-   super bouncy
-   drift points (with no benefit)
-   money from style points (making the drift points relic useful)
-   money per car flip
-   money per second spent in races
-   increased money per race won
-   reduction in tyre wear
-   reduction in fuel use
-   relics that need distance travelled to get better
-   improve based on part branding based on the amount of a brand of parts you have
-   every 5 minutes your power/grip/drag gets randomized in some range
-   temporarily upgrades your car for a limited amount of time

---

## Circuit game idea:

-   You are a test track driver
-   And have to select parts to make your car go faster around the fixed track

    -   Maybe target time slowly reduces under financial pressure?

-   Track should be a circuit so you can try 3 laps for the best time

-   Get slightly more money the faster you go - although not sure what money would be for yet

-   Parts are shown on the model
    -   I would apply all parts to the model and start the game with them all hidden

and its general TODO list:

some simple circuit generation:

-   probably prod the dynamic world generation as i can't make a track to save my life
-   with checkpoints and lap time calc

parts

-   no 3d models needed yet, but would be nice
-   using the engine.py on the desktop for now

UI screens (in order):

-   intro screen (once)
-   level start screen + your goal is blah
-   racing screen
-   racing results screen
-   upgrade screen for selecting parts
    -   at least a text block for displaying new car stats

Some way to calculate the starting time to beat

-   maybe get AI to drive it in the future

---

## Misc

/_
Unused property colours:
#fabed4
#fffac8
_/

current theme colours

https://uicolors.app/create

'mandy': {
'50': '#fef2f3', 0.996, 0.949, 0.957
'100': '#fde6e7', 0.992, 0.902, 0.906
'200': '#fbd0d5', 0.984, 0.816, 0.835
'300': '#f7aab2', 0.969, 0.667, 0.698
'400': '#f27a8a', 0.949, 0.478, 0.541
'500': '#ea546c', 0.918, 0.329, 0.424
'600': '#d5294d', 0.835, 0.165, 0.302
'700': '#b31d3f', 0.702, 0.114, 0.247
'800': '#961b3c', 0.588, 0.106, 0.239
'900': '#811a39', 0.506, 0.106, 0.224
'950': '#48091a', 0.282, 0.035, 0.106
},
