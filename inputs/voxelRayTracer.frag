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

uniform float time;

uniform int mouseInput;

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
float voxelSize = 0.25f;

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
int flatten(ivec3 pos)
{
	return pos.z * (width * height) + pos.y * width + pos.x;
}

int voxelRaycast(Ray ray, out vec3 hitPoint, out vec3 lastVoxel)
{
    Ray rayCast = Ray(ray.origin / voxelSize, ray.dir / voxelSize);
    vec3 invDir = 1.0 / rayCast.dir;
    vec3 tDelta = abs(invDir);
    vec3 step = sign(rayCast.dir);

    AABB box = AABB(vec3(0.0), vec3(width, height, depth));
    vec3 t0 = (box.min - rayCast.origin) * invDir;
    vec3 t1 = (box.max - rayCast.origin) * invDir;
    vec3 tSmall = min(t0, t1);
    vec3 tBig   = max(t0, t1);
    float tMin = max(max(tSmall.x, tSmall.y), tSmall.z);
    float tMax = min(min(tBig.x, tBig.y), tBig.z);
    float hitT = tMin;
    if (tMax < 0.0 || tMin > tMax) return -1;

    tMin = max(tMin, 0.0);

    vec3 currentStep = floor(rayCast.origin + rayCast.dir * tMin);
    vec3 planes = currentStep + max(step, 0.0);
    vec3 t = (planes - rayCast.origin) * invDir;
    vec3 previousStep = currentStep;
    while(true)
    {
        if (inBounds(currentStep))
        {
            int index = flatten(ivec3(currentStep)); 
            if (voxels[index] != 0)
            {
                hitPoint = (rayCast.origin + rayCast.dir * hitT) * voxelSize;
                lastVoxel = previousStep;
                return index;
            }
        }

        previousStep = currentStep;
        if (t.x < t.y && t.x < t.z)
        {
            hitT = t.x;
            currentStep.x += step.x;
            t.x += tDelta.x;
        }
        else if (t.y < t.z)
        {
            hitT = t.y;
            currentStep.y += step.y;
            t.y += tDelta.y;
        }
        else
        {
            hitT = t.z;
            currentStep.z += step.z;
            t.z += tDelta.z;
        }

        if (currentStep.x >= width || currentStep.y >= height || currentStep.z >= depth) return -1;
        if (currentStep.x < 0 || currentStep.y < 0 || currentStep.z < 0) return -1;
    }
}
int voxelTraversal(Ray ray, out vec3 normal, out vec3 voxelPos, out int voxelIndex)
{
    Ray rayCast = Ray(ray.origin / voxelSize, ray.dir / voxelSize);
    vec3 invDir = 1.0 / rayCast.dir;
    vec3 tDelta = abs(invDir);
    vec3 step = sign(rayCast.dir);

    AABB box = AABB(vec3(0.0), vec3(width, height, depth));
    vec3 t0 = (box.min - rayCast.origin) * invDir;
    vec3 t1 = (box.max - rayCast.origin) * invDir;
    vec3 tSmall = min(t0, t1);
    vec3 tBig   = max(t0, t1);
    float tMin = max(max(tSmall.x, tSmall.y), tSmall.z);
    float tMax = min(min(tBig.x, tBig.y), tBig.z);
    float hitT = tMin;
    if (tMax < 0.0 || tMin > tMax) return 0;

    if (tMin == tSmall.x)      normal = vec3(-step.x, 0.0, 0.0);
    else if (tMin == tSmall.y) normal = vec3(0.0, -step.y, 0.0);
    else                       normal = vec3(0.0, 0.0, -step.z);

    tMin = max(tMin, 0.0);

    vec3 currentStep = floor(rayCast.origin + rayCast.dir * tMin);
    vec3 planes = currentStep + max(step, 0.0);
    vec3 t = (planes - rayCast.origin) * invDir;

    voxelIndex = -1;
    while(true)
    {
        if (inBounds(currentStep))
        {
            voxelPos = (rayCast.origin + rayCast.dir * hitT) * voxelSize;
            voxelIndex = flatten(ivec3(currentStep));

            int voxelMat = voxels[voxelIndex];
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
    int voxelIndex;
    int mat = voxelTraversal(Ray(camPos, rayDir), normal, voxelPos, voxelIndex);

    float radius = 2;
    float dMaxDistance = 50.0;
    float bMinDistance = 10.0;

    vec2 clampedUV = clamp(uv, -radius, radius);
    vec3 hitPoint;
    vec3 lastHitPoint;

    int lookedAt = voxelRaycast(Ray(camPos + (uv.x * camRight + uv.y * camUp), camDir), hitPoint, lastHitPoint);
    if (lookedAt != -1 && mouseInput == 1 && length(hitPoint - camPos) < dMaxDistance) voxels[lookedAt] = 0;
    if (lookedAt != -1 && mouseInput == -1 && length(hitPoint - camPos) > bMinDistance && inBounds(floor(lastHitPoint))) voxels[flatten(ivec3(lastHitPoint))] = voxels[lookedAt];

    if (mat == 0) FragColor = vec4(0.1, 0.12, 0.1, 1);
    else
    {
	    float ampientStrength = 0.2;
        float shadow = 1;

        if (dot(normal, -lightDir) >= 0.0)
        {
            vec3 dum1;
            vec3 dum2;
            int voxelLightHit = voxelRaycast(Ray(voxelPos + normal * 0.4, -lightDir), dum1, dum2);
            shadow = voxelLightHit == -1 ? 1 : 0.2;
        }

	    vec3 viewDir    = normalize(camPos - voxelPos);
	    vec3 reflectDir = reflect(normalize(-lightDir), normalize(normal));

	    vec3 diffuse = lightColor * max(dot(normalize(normal), normalize(-lightDir)), 0.0) * shadow;
	    vec3 ambient = ampientStrength * lightColor;

        FragColor = vec4(materials[mat - 1] * (diffuse + ambient), 1);
    }
}
