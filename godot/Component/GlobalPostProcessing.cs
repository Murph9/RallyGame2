using Godot;

namespace murph9.RallyGame2.godot.Component;

public partial class GlobalPostProcessing : Node3D {

    private const float MAX_CULL_MARGIN = 16384;

    private Camera3D _current3dCamera;
    private MeshInstance3D _shaderQuad;

    public override void _Ready() {
        _current3dCamera = GetViewport().GetCamera3D();

        // create a quad and load the global shader from the docs
        // https://docs.godotengine.org/en/stable/tutorials/shaders/advanced_postprocessing.html#full-screen-quad
        _shaderQuad = new MeshInstance3D() {
            Mesh = new QuadMesh() {
                FlipFaces = true,
                Size = new Vector2(2, 2)
            },
            MaterialOverride = new ShaderMaterial() {
                Shader = GD.Load<Shader>("res://HighlightEdgePostProssessing.gdshader")
            },
            // NOTE: anything that is transparent and further than MAX_CULL_MARGIN will not be rendered
            Transform = new Transform3D(Basis.Identity, Vector3.Forward * (MAX_CULL_MARGIN - 1)),
            ExtraCullMargin = MAX_CULL_MARGIN
        };
    }

    public override void _Process(double delta) {
        // sometimes the camera can be changed (like when reloading the car)
        // so detect that and re-add the shader mesh instance
        var currentCamera = GetViewport().GetCamera3D();
        if (currentCamera == _current3dCamera) {
            return;
        }

        // then clean up
        _current3dCamera?.RemoveChild(_shaderQuad);

        // add add it to the new camera
        _current3dCamera = currentCamera;
        _current3dCamera?.AddChild(_shaderQuad);
    }
}
