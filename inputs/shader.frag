#version 330 core
out vec4 FragColor;

in vec2 uv;

uniform sampler2D texture1;
uniform sampler2D texture2;

void main()
{
    float gradiant = uv.x < 0.5 ? 2 * uv.x * uv.x: 1 - pow(-2 * uv.x + 2, 2) / 2;

    FragColor = mix(texture(texture1, uv), texture(texture2, uv), gradiant);
}