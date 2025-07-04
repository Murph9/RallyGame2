# RallyGame2 Readme

## Project setup

Download godot, setup the .vscode launch.json script to point to its location

---

## 100km Distance Game Idea:

An infinite world that has:

-   goal of driving 100km
-   spawn next to an infinite road
-   at some fixed amount X km you will have a goal to reach:
    -   a speed trap target
    -   an average speed section
    -   time trial section
    -   ???
-   at every new section you must pick a new world section:
    -   pick between like 2 kinds of new road type
    -   each have a specific category of reward to make you pick certain kinds
    -   possibly a weather condition like wind/air density/rain/slippery/more expensive
        -   more built up, i.e. more things but costs more
    -   the next goal should be known for each one as well
-   NEW: it might be better to force racing other cars
    -   and boss cars which are faster get you relics
    -   a top speed/accel/handling estimate performance numbers for rivals
-   start with a base car but slowly make it better
-   come across cars to race along the road for $
    -   match their speed and challenge them
        -   win and get a random part (with rarity)
        -   if you lose you get nothing
    -   also get money per race
    -   maybe they see you and try and match your speed
-   traffic
-   shops spawn along route
    -   maybe you can open a shop at any point
-   nos comes back by driving dangerously (arcade nos)
-   tyre wear
-   fuel use
-   body damage from collisions (which affects performance somehow)

Ideas for relics not created yet:

-   relic that upgrades the road type
    -   you know how peglin has 3 phases? why don't we have upgraded road types
    -   or road types are a part of the challenge, pick directions and it may slowly get there
-   relics allow you to buy certain upgrades only
-   nos refills while idling (no accel/braking)
-   nos refills based on speed
-   money for continually accelerating
-   reduced damage from collisions
-   money for near misses

-   Some relic misc categories:

    -   that get unlocked by having certain parts (and maybe not others?)
    -   that need distance travelled to get better
    -   improve based on part branding based on the amount of a brand of parts you have
    -   every 5 minutes your power/grip/drag gets randomized in some range
    -   temporarily upgrades your car for a limited amount of time
    -   parts that break after certain time

Ideas for the world generation:

-   use MeshDataTool to perturb them a little for better variance

Graphics ideas:

-   https://www.reddit.com/r/ImaginaryTechnology/comments/1gko5z4/back_to_the_future_3d_animation_by_me/

Ideas for more goals:

-   flip your car somewhere
-   hit differently coloured traffic
-   certain goal counts get you relics

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
