using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using System.Linq;

namespace murph9.RallyGame2.godot.PayDay.Hub;

/// <summary>
/// Lightweight display-only car node. Loads the car's blend mesh and wheels
/// at their stored positions — no physics, no simulation.
/// Call Initialise(CarDetails) before adding to the scene tree.
/// </summary>
public partial class CarDisplayNode : StaticBody3D {

    private CarDetails _details;

    public void Initialise(CarDetails details) {
        _details = details;

        SetMeta(nameof(HubItemType), "Car");
    }

    public override void _Ready() {
        if (_details == null)
            return;

        // Load and instantiate the car blend file
        var scene = GD.Load<PackedScene>("res://assets/car/" + _details.CarModel);
        var carScene = scene.Instantiate<Node3D>();

        // Pull out the RigidBody3D which contains the mesh children
        var rigidBody = carScene.GetChildren().OfType<RigidBody3D>().FirstOrDefault() ?? throw new System.Exception("Car rigid body not found");

        // Re-parent just the MeshInstance3D children — skip collision shapes
        var meshes = rigidBody.GetChildren().OfType<MeshInstance3D>().ToList();
        foreach (var mesh in meshes) {
            mesh.GetParent().RemoveChild(mesh);
            mesh.Owner = null;

            // Apply car colour to [primary] surfaces
            ApplyColour(mesh);

            AddChild(mesh);
        }

        // pull out the collision shapes
        var cols = rigidBody.GetChildren().OfType<CollisionShape3D>().ToList();
        foreach (var col in cols) {
            col.GetParent().RemoveChild(col);
            col.Owner = null;

            AddChild(col);
        }

        // Discard the original scene wrapper
        carScene.QueueFree();

        // Add wheel meshes at their stored positions
        foreach (var wheelDetails in _details.WheelDetails) {
            var wheelScene = GD.Load<PackedScene>("res://assets/car/" + wheelDetails.ModelName);
            if (wheelScene == null) continue;

            var wheelNode = wheelScene.Instantiate<Node3D>();

            // Pull the first MeshInstance3D out of the wheel scene
            var wheelMesh = wheelNode.GetChildren().OfType<RigidBody3D>().FirstOrDefault()
                               ?.GetChildren().OfType<MeshInstance3D>().FirstOrDefault()
                           ?? wheelNode.GetChildren().OfType<MeshInstance3D>().FirstOrDefault();

            if (wheelMesh != null) {
                wheelMesh.GetParent().RemoveChild(wheelMesh);
                wheelMesh.Owner = null;
                wheelMesh.Position = wheelDetails.Position;
                AddChild(wheelMesh);
            }

            wheelNode.QueueFree();
        }
    }

    private static void ApplyColour(MeshInstance3D mesh) {
        if (mesh.Mesh == null)
            return;

        // Match the colour logic in Car.cs — grey default
        var colour = new Color(0.8f, 0.8f, 0.8f, 1f);
        for (var i = 0; i < mesh.Mesh.GetSurfaceCount(); i++) {
            var material = mesh.GetActiveMaterial(i);
            // Check ResourceName on the original material before duplicating
            if (material == null || !material.ResourceName.Contains("[primary]"))
                continue;
            if (material is not StandardMaterial3D srcMat)
                continue;

            var newMat = (StandardMaterial3D)srcMat.Duplicate();
            newMat.AlbedoColor = colour;
            mesh.SetSurfaceOverrideMaterial(i, newMat);
        }
    }
}
