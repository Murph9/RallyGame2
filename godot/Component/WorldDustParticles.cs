using Godot;

namespace murph9.RallyGame2.godot.Component;

public partial class WorldDustParticles : Node3D {

    private Camera3D _current3dCamera;
    private GpuParticles3D _particles;

    public override void _Ready() {
        _current3dCamera = GetViewport().GetCamera3D();

        _particles = new GpuParticles3D() {
            ProcessMaterial = new ParticleProcessMaterial() {
                EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
                EmissionShapeScale = new Vector3(100, 100, 100),
                Color = Colors.WhiteSmoke,
                Gravity = new Vector3(0, 0, 0),
            },
            Lifetime = 30,
            Amount = 10000,
            DrawPass1 = new QuadMesh() { Size = new Vector2(0.05f, 0.05f) },
        };
    }

    public override void _Process(double delta) {
        // sometimes the camera can be changed (like when reloading the car)
        // so detect that and re-add the particles
        var currentCamera = GetViewport().GetCamera3D();
        if (currentCamera == _current3dCamera) {
            return;
        }

        // then clean up
        _current3dCamera?.RemoveChild(_particles);

        // add add it to the new camera
        _current3dCamera = currentCamera;
        _current3dCamera?.AddChild(_particles);
    }
}