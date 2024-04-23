shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable;

void fragment() {
	COLOR = texture(screen_texture, SCREEN_UV);
	COLOR.a = 1.0 - COLOR.r;
}


