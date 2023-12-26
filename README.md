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
