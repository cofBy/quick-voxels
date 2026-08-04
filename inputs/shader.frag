#version 330 core
out vec4 FragColor;

in vec3 normal;
in vec3 fragPos;

uniform vec3 objectColor;
uniform vec3 lightColor;
uniform vec3 lightPos;
uniform vec3 camPos;

void main()
{
	float ampientStrength  = 0.2;
	float specularStrength = 1.0;

	vec3 lightDir = normalize(lightPos - fragPos);
	vec3 viewDir  = normalize(camPos - fragPos);
	vec3 reflectDir = reflect(-lightDir, normalize(normal));

	vec3 diffuse  = lightColor * max(dot(normalize(normal), lightDir), 0.0);
	vec3 ambient  = ampientStrength * lightColor;
	vec3 specular = specularStrength * pow(max(dot(viewDir, reflectDir), 0.0), 32) * lightColor;

	FragColor = vec4(objectColor * (diffuse + ambient + specular), 1.0);
}