#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aUV;

out vec2 uv;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    uv = aUV;
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
}