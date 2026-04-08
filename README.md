# RallyGame2 Readme

## Project setup

Download godot, setup the .vscode launch.json script to point to its location

---

## Pay Day Loan Idea

Basic Idea is that you are paying off pay day loan or rent to own scheme for some stupid appliance, you race collect parts and money every 'day' to come home and apply to the car

Some other notes:

- Parts have a colour based rarity with camera and animation to apply to the car
- the run had the loan gets worse every day - terrible terms and conditions
- Talk to friends about progress and they are the narrator
    - Set up info about how are you going to pay for this jokes about making drugs but ultimately you aren't a chemist
    - Really double down on the rarity jokes
- Global progress is a glowy home - which exists in 3d space as the menu
- Do house parts cost different amounts or is it random and just difficulty
- Car parts also glow with the same rarity system
- How long are the nights? A few minutes? Need to calculate how long it takes to win a few
- Choose between money and parts to finish the loan

MVP:

- use one type of car
- a house with grey/colourable furniture
    - which is a menu that is clickable
- a single type of infinite road that just ends on the timer
- probably some dialog for the start
- come across cars to race along the road for $ or parts
- shaders that we can apply to all the parts/furniture which make it rarer
    - honestly we might just copy looter games:
      white (common), green (uncommon), blue (rare), purple (epic) or orange (legendary)
    - stats can stay static for now

- TODOs for MVP:
    - traffic cars should disappear hitting the end barrier (because they have no target)
    - modify car screen should show car stats
    - challenging traffic should show their relative stats to yours
        - challenging cars should have a downside
    - car ai should be better :[

Nice product stuff:

- global across run stat changes (and rouge lite features) including but not limited to:
    - distance travelled, rivals fought + won/lost, upgrades collected, highest level, time off the ground
    - with global goals like alto's adventure
    - which unlocks: new car skins, new couches and house items
- multiple cars to use
- environments that you have to buy parts for
- part upgrades that aren't just: its better

---

## 100km Distance Game Idea:

An infinite world that has:

- goal of driving 100km
- spawn next to an infinite road
- at some fixed amount X km you will have a goal to reach:
    - a speed trap target
    - an average speed section
    - time trial section
    - ???
- at every new section you must pick a new world section:
    - pick between like 2 kinds of new road type
    - each have a specific category of reward to make you pick certain kinds
    - possibly a weather condition like wind/air density/rain/slippery/more expensive
        - more built up, i.e. more things but costs more
    - the next goal should be known for each one as well
- NEW: it might be better to force racing other cars
    - and boss cars which are faster get you relics
    - a top speed/accel/handling estimate performance numbers for rivals
- start with a base car but slowly make it better
- come across cars to race along the road for $
    - match their speed and challenge them
        - win and get a random part (with rarity)
        - if you lose you get nothing
    - also get money per race
    - maybe they see you and try and match your speed
- traffic
- shops spawn along route
    - maybe you can open a shop at any point
- nos comes back by driving dangerously (arcade nos)
- tyre wear
- fuel use
- body damage from collisions (which affects performance somehow)

Ideas for relics not created yet:

- relic that upgrades the road type
    - you know how peglin has 3 phases? why don't we have upgraded road types
    - or road types are a part of the challenge, pick directions and it may slowly get there
- relics allow you to buy certain upgrades only
- nos refills while idling (no accel/braking)
- nos refills based on speed
- money for continually accelerating
- reduced damage from collisions
- money for near misses

- Some relic misc categories:
    - that get unlocked by having certain parts (and maybe not others?)
    - that need distance travelled to get better
    - improve based on part branding based on the amount of a brand of parts you have
    - every 5 minutes your power/grip/drag gets randomized in some range
    - temporarily upgrades your car for a limited amount of time
    - parts that break after certain time

Ideas for the world generation:

- use MeshDataTool to perturb them a little for better variance

Graphics ideas:

- https://www.reddit.com/r/ImaginaryTechnology/comments/1gko5z4/back_to_the_future_3d_animation_by_me/

Ideas for more goals:

- flip your car somewhere
- hit differently coloured traffic
- certain goal counts get you relics

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
