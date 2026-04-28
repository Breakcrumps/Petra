shader_type spatial;
render_mode unshaded, blend_add, depth_draw_opaque;

uniform vec4 color : source_color = vec4(1.f, .3f, .1f, 1.f);
uniform float glow_intensity : hint_range(1.f, 100.f) = 100.f;

void fragment()
{
  float horizontal_fade = smoothstep(.0f, .5f, UV.x) * smoothstep(1.f, .5f, UV.x);
  float vertical_fade = smoothstep(.0f, .7f, UV.y);
  float mask = horizontal_fade * vertical_fade;
  ALBEDO = color.rgb * glow_intensity;
  ALPHA = color.a * mask;
}
