# RallyGame2 Readme

## Project setup

Download godot, setup the .vscode launch.json script to point to its location

Godotjolt is already downloaded in the repo as a zip in the guid folder /godot/9bb...


## How Tos

### Adding a model with physics
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


----

Current game idea:

- You are a test track driver
- And have to select parts to make your car go faster around the fixed track
  - Maybe target time slowly reduces under financial pressure?

- Track should be a circuit so you can try 3 laps for the best time

- Get slightly more money the faster you go - although not sure what money would be for yet

- Parts are shown on the model
  - I would apply all parts to the model and start the game with them all hidden

and its TODO list:

some simple circuit generation:
- probably prod the dynamic world generation as i can't make a track to save my life
- with checkpoints and lap time calc

parts
- no 3d models needed yet, but would be nice
- using the engine.py on the desktop for now

a UI for selecting parts
- at least a text block for displaying car stats

Some way to calculate the starting time to beat
- maybe get AI to drive it in the future

---

/*
Unused property colours:
#42d4f4
#4363d8
#911eb4
#f032e6
#fabed4
#fffac8
#dcbeff
*/
