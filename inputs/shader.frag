#version 330 core
out vec4 FragColor;

in vec3 normal;
in vec3 fragPos;

struct Material
{
	vec3 objectColor;
	float specularStrength;
};
uniform Material myMat;

uniform vec3 lightColor;
uniform vec3 lightDir;
uniform vec3 camPos;

void main()
{
	float ampientStrength  = 0.2;

	vec3 viewDir  = normalize(camPos - fragPos);
	vec3 reflectDir = reflect(-normalize(lightDir), normalize(normal));

	vec3 diffuse  = lightColor * max(dot(normalize(normal), normalize(lightDir)), 0.0);
	vec3 ambient  = ampientStrength * lightColor;
	vec3 specular = myMat.specularStrength * pow(max(dot(viewDir, reflectDir), 0.0), 16) * lightColor;

	FragColor = vec4(myMat.objectColor * (diffuse + ambient + specular), 1.0);
}