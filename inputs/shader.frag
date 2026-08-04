#version 330 core
out vec4 FragColor;

in vec3 normal;
in vec3 fragPos;

uniform vec3 objectColor;
uniform vec3 lightColor;
uniform vec3 lightPos;

void main()
{
	vec3 lightDir = normalize(lightPos - fragPos);

	vec3 diffuse = lightColor * max(dot(normalize(normal), lightDir), 0.0);

	FragColor = vec4(objectColor * diffuse, 1.0);
}