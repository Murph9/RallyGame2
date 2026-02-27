shader_type spatial;

uniform vec4 rarity_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float glow_strength : hint_range(0.0, 1.0) = 0.0;
uniform sampler2D texture_albedo : source_color, hint_default_white;

void fragment() {
    vec4 albedo_tex = texture(texture_albedo, UV);
    ALBEDO = albedo_tex.rgb;
    EMISSION = rarity_color.rgb * glow_strength;
    ALPHA = albedo_tex.a;
}
