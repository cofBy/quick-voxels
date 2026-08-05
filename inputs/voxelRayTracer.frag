#version 430 core
out vec4 FragColor;

in vec2 fragPos;

uniform vec3 camPos;
uniform vec3 camDir;
uniform vec3 camUp;
uniform vec3 camRight;
uniform vec3 lightColor;
uniform vec3 lightDir;
uniform vec2 res;

vec3[] materials =
{
    vec3(0.31, 1.00, 0.23),
    vec3(0.34, 0.20, 0.02),
    vec3(0.20, 0.22, 0.21)
};

layout(std430, binding = 0) buffer voxelBuffer { int voxels[]; };
uniform int width;
uniform int height;
uniform int depth;

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
    return all(greaterThanEqual(pos, vec3(0.0))) && all(lessThan(pos, vec3(width, height, depth)));
}
int flatten(vec3 pos)
{
	return int(pos.z * (width * height) + pos.y * width + pos.x);
}

int voxelTraversal(Ray ray, out vec3 normal, out vec3 voxelPos)
{
    vec3 invDir = 1.0 / ray.dir;
    vec3 tDelta = abs(invDir);
    vec3 step = sign(ray.dir);
    float hitT = 0;

    AABB box = AABB(vec3(0.0), vec3(width, height, depth));
    vec3 t0 = (box.min - ray.origin) * invDir;
    vec3 t1 = (box.max - ray.origin) * invDir;
    vec3 tSmall = min(t0, t1);
    vec3 tBig   = max(t0, t1);
    float tMin = max(max(tSmall.x, tSmall.y), tSmall.z);
    float tMax = min(min(tBig.x, tBig.y), tBig.z);
    if (tMax < 0.0 || tMin > tMax) return 0;

    if (tMin == tSmall.x)      normal = vec3(-step.x, 0.0, 0.0);
    else if (tMin == tSmall.y) normal = vec3(0.0, -step.y, 0.0);
    else                       normal = vec3(0.0, 0.0, -step.z);

    tMin = max(tMin, 0.0);

    vec3 currentStep = floor(ray.origin + ray.dir * tMin);
    vec3 planes = currentStep + max(step, 0.0);
    vec3 t = (planes - ray.origin) * invDir;

    while(true)
    {
        if (inBounds(currentStep))
        {
            int voxelMat = voxels[flatten(floor(currentStep))];
            voxelPos = ray.origin + ray.dir * hitT;
            if (voxelMat != 0) return voxelMat;
        }

        if (t.x < t.y && t.x < t.z)
        {
            hitT = t.x;
            currentStep.x += step.x;
            t.x += tDelta.x;
            normal = vec3(-step.x, 0, 0);
        }
        else if (t.y < t.z)
        {
            hitT = t.y;
            currentStep.y += step.y;
            t.y += tDelta.y;
            normal = vec3(0, -step.y, 0);
        }
        else
        {
            hitT = t.z;
            currentStep.z += step.z;
            t.z += tDelta.z;
            normal = vec3(0, 0, -step.z);
        }

        if (currentStep.x >= width || currentStep.y >= height || currentStep.z >= depth) return 0;
        if (currentStep.x < 0 || currentStep.y < 0 || currentStep.z < 0) return 0;
    }
}

void main()
{
    float aspect = res.x / res.y;
    vec2 uv = fragPos * vec2(aspect, 1.0);
    vec3 rayDir = normalize(camDir + uv.x * camRight + uv.y * camUp);

    vec3 normal;
    vec3 voxelPos;
    int mat = voxelTraversal(Ray(camPos, rayDir), normal, voxelPos);
    if (mat == 0)
    {
	    FragColor = vec4(0.1, 0.12, 0.1, 1);
    }
    else
    {
	    float ampientStrength  = 0.2;

	    vec3 viewDir    = normalize(camPos - voxelPos);
	    vec3 reflectDir = reflect(normalize(-lightDir), normalize(normal));

	    vec3 diffuse  = lightColor * max(dot(normalize(normal), normalize(-lightDir)), 0.0);
	    vec3 ambient  = ampientStrength * lightColor;

        FragColor = vec4(materials[mat - 1] * (diffuse + ambient), 1);
    }
}
