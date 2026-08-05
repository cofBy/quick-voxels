#version 430 core
out vec4 FragColor;

in vec2 fragPos;

uniform vec3 camPos;
uniform vec3 camDir;
uniform vec3 camUp;
uniform vec3 camRight;
uniform vec2 res;

struct Material
{
    vec3 color;
    float shininess;
};
Material[] materials =
{
    Material(vec3(0.1, 0.05, 0.2 ), 0.2),
    Material(vec3(0.8, 0.5 , 0.05), 0.8),
    Material(vec3(0.1, 0.3 , 0.8 ), 1.0)
};

layout(std430, binding = 0) buffer voxelBuffer { int voxels[]; };
uniform vec3 voxelRes;

struct Ray
{
    vec3 origin;
    vec3 dir;
};
struct AABB
{
    vec3 min;
    vec3 max;
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
    vec3 invDir = 1.0 / ray.dir;

    AABB box = AABB(vec3(0.0), voxelRes);
    vec3 t0 = (box.min - ray.origin) * invDir;
    vec3 t1 = (box.max - ray.origin) * invDir;
    vec3 tSmall = min(t0, t1);
    vec3 tBig   = max(t0, t1);
    float tMin = max(max(tSmall.x, tSmall.y), tSmall.z);
    float tMax = min(min(tBig.x, tBig.y), tBig.z);
    if (tMax < 0.0 || tMin > tMax) return 0;
    tMin = max(tMin, 0.0);

    vec3 tDelta = abs(invDir);
    vec3 step = sign(ray.dir);
    vec3 currentStep = floor(ray.origin + ray.dir * tMin);
    vec3 planes = currentStep + max(step, 0.0);
    vec3 t = (planes - ray.origin) * invDir;

    while(true)
    {
        if (inBounds(currentStep))
        {
            int voxelMat = voxels[flatten(floor(currentStep))];
            if (voxelMat != 0) return voxelMat;
        }

        if (t.x < t.y && t.x < t.z)
        {
            currentStep.x += step.x;
            t.x += tDelta.x;
        }
        else if (t.y < t.z)
        {
            currentStep.y += step.y;
            t.y += tDelta.y;
        }
        else
        {
            currentStep.z += step.z;
            t.z += tDelta.z;
        }

        if (currentStep.x >= voxelRes.x || currentStep.y >= voxelRes.y || currentStep.z >= voxelRes.z) return 0;
        if (currentStep.x < 0 || currentStep.y < 0 || currentStep.z < 0) return 0;
    }
}

void main()
{
    float aspect = res.x / res.y;
    vec2 uv = fragPos * vec2(aspect, 1.0);
    vec3 rayDir = normalize(camDir + uv.x * camRight + uv.y * camUp);

    int mat = voxelTraversal(Ray(camPos, rayDir));
    if (mat == 0)
    {
	    FragColor = vec4(0.1, 0.12, 0.1, 1);
    }
    else
    {
        FragColor = vec4(materials[mat - 1].color, 1);
    }
}
