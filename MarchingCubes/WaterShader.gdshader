shader_type spatial;

uniform sampler2D depth_tex : hint_depth_texture;
uniform sampler2D albedo_tex : hint_screen_texture;
uniform sampler2D noise;

uniform float depth_multiplier: hint_range(0.01, 1.0, 0.01);
uniform float surface_multiplier: hint_range(0.01, 1.0, 0.01);
uniform float depth_value_multiplier : hint_range(0.01, 1.0, 0.01);
uniform float noise_color_multiplier : hint_range(0.01, 1.0, 0.01);

uniform vec3 water_color : source_color;

void vertex() {
}


void fragment() {
	float mult = depth_multiplier;
	float depth = texture(depth_tex, SCREEN_UV).r;
	float noise_amt = texture(noise, UV + vec2(sin(TIME * 0.015) * cos(TIME * 0.015))).r * noise_color_multiplier;
	depth = PROJECTION_MATRIX[3][2] / (depth + PROJECTION_MATRIX[2][2]);
	depth = (VERTEX * NORMAL).y + depth * depth_value_multiplier;
	vec3 surface_color = texture(albedo_tex, SCREEN_UV).rgb;

	ALBEDO = (surface_color * surface_multiplier) + (water_color * (abs(depth) * depth_multiplier) + noise_amt);

}
