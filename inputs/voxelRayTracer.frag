#version 430 core
out vec4 FragColor;

in vec2 fragPos;

uniform vec3 camPos;
uniform vec3 camDir;
uniform vec3 camUp;
uniform vec3 camRight;
uniform vec2 res;

layout(std430, binding = 0) buffer voxelBuffer { int voxels[]; };
uniform vec3 voxelRes;

struct Ray
{
    vec3 origin;
    vec3 dir;
};

bool inBounds(vec3 pos)
{
    return all(greaterThanEqual(pos, vec3(0.0))) && all(lessThan(pos, voxelRes));
}
int flatten(vec3 pos)
{
	return int(pos.z * (voxelRes.x * voxelRes.y) + pos.y * voxelRes.x + pos.x);
}
int voxelTraversal(Ray ray)
{
    float step = 0.05;
    float maxDistance = 20.0;

    vec3 currentStep = ray.origin;

    while (length(currentStep - ray.origin) <= maxDistance)
    {
        vec3 voxelPos = floor(currentStep);
        if (inBounds(voxelPos))
        {
            int v = voxels[flatten(voxelPos)];
            if (v != 0) return v;
        }
        currentStep += ray.dir * step;
    }
    return 0;
}

void main()
{
    float aspect = res.x / res.y;
    vec2 uv = fragPos * vec2(aspect, 1.0);
    vec3 rayDir = normalize(camDir + uv.x * camRight + uv.y * camUp);

    if (voxelTraversal(Ray(camPos, rayDir)) == 0)
    {
	    FragColor = vec4(0.1, 0.12, 0.1, 1);
    }
    else
    {
        FragColor = vec4(1);
    }
}
