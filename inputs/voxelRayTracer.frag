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
uniform int buildMat;

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
float voxelSize = 0.1f;

layout(std430, binding = 1) buffer brickBuffer { int brickCounts[]; };

const int BRICK = 16;
uniform int brickW, brickH, brickD;

int brickFlatten(ivec3 b) { return b.z * (brickW * brickH) + b.y * brickW + b.x; }
bool brickInBounds(ivec3 b) { return all(greaterThanEqual(b, ivec3(0))) && b.x < brickW && b.y < brickH && b.z < brickD; }
bool brickOccupied(ivec3 b) { return brickInBounds(b) && brickCounts[brickFlatten(b)] != 0; }

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
bool isSolid(ivec3 pos)
{
    return inBounds(vec3(pos)) && voxels[flatten(pos)] != 0;
}
void faceBasis(vec3 normal, out ivec3 tangentU, out ivec3 tangentV)
{
    if (abs(normal.x) > 0.5)      { tangentU = ivec3(0,0,1); tangentV = ivec3(0,1,0); }
    else if (abs(normal.y) > 0.5) { tangentU = ivec3(1,0,0); tangentV = ivec3(0,0,1); }
    else                          { tangentU = ivec3(1,0,0); tangentV = ivec3(0,1,0); }
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
    currentStep = clamp(currentStep, vec3(0.0), vec3(width, height, depth) - vec3(1.0));
    vec3 planes = currentStep + max(step, 0.0);
    vec3 t = (planes - rayCast.origin) * invDir;
    vec3 previousStep = currentStep;

    int steps = 0;
    int maxSteps = width + height + depth;
    while(steps < maxSteps)
    {
        steps++;
        if (inBounds(currentStep))
        {
            ivec3 brickCoord = ivec3(currentStep / float(BRICK));
            if (!brickOccupied(brickCoord))
            {
                vec3 brickMin = vec3(brickCoord * BRICK);
                vec3 brickMax = brickMin + vec3(BRICK);
                vec3 bt1 = max((brickMin - rayCast.origin) * invDir, (brickMax - rayCast.origin) * invDir);
                float exitT = min(min(bt1.x, bt1.y), bt1.z) + 1e-3;
                vec3 exitPoint = rayCast.origin + rayCast.dir * exitT;

                previousStep = currentStep;

                if (bt1.x <= bt1.y && bt1.x <= bt1.z)
                {
                    currentStep.x = step.x > 0.0 ? brickMax.x : brickMin.x - 1.0;
                    currentStep.y = floor(exitPoint.y);
                    currentStep.z = floor(exitPoint.z);
                }
                else if (bt1.y <= bt1.z)
                {
                    currentStep.y = step.y > 0.0 ? brickMax.y : brickMin.y - 1.0;
                    currentStep.x = floor(exitPoint.x);
                    currentStep.z = floor(exitPoint.z);
                }
                else
                {
                    currentStep.z = step.z > 0.0 ? brickMax.z : brickMin.z - 1.0;
                    currentStep.x = floor(exitPoint.x);
                    currentStep.y = floor(exitPoint.y);
                }

                hitT = exitT;
                vec3 planes2 = currentStep + max(step, 0.0);
                t = (planes2 - rayCast.origin) * invDir;
            }
            else
            {
                int index = flatten(ivec3(currentStep));
                if (voxels[index] != 0)
                {
                    hitPoint = (rayCast.origin + rayCast.dir * hitT) * voxelSize;
                    lastVoxel = previousStep;
                    return index;
                }

                previousStep = currentStep;
                if (t.x < t.y && t.x < t.z) { hitT = t.x; currentStep.x += step.x; t.x += tDelta.x; }
                else if (t.y < t.z)         { hitT = t.y; currentStep.y += step.y; t.y += tDelta.y; }
                else                        { hitT = t.z; currentStep.z += step.z; t.z += tDelta.z; }
            }
        }

        if (currentStep.x >= width || currentStep.y >= height || currentStep.z >= depth) return -1;
        if (currentStep.x < 0 || currentStep.y < 0 || currentStep.z < 0) return -1;
    }
    return -1;
}
int voxelTraversal(Ray ray, out vec3 normal, out vec3 voxelPos, out vec2 uv, out ivec3 voxelCoord)
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
    currentStep = clamp(currentStep, vec3(0.0), vec3(width, height, depth) - vec3(1.0));
    vec3 planes = currentStep + max(step, 0.0);
    vec3 t = (planes - rayCast.origin) * invDir;
    int steps = 0;
    int maxSteps = width + height + depth;
    while(steps < maxSteps)
    {
        steps++;
        if (inBounds(currentStep))
        {
            ivec3 brickCoord = ivec3(currentStep / float(BRICK));
            if (!brickOccupied(brickCoord))
            {
                vec3 brickMin = vec3(brickCoord * BRICK);
                vec3 brickMax = brickMin + vec3(BRICK);
                vec3 bt1 = max((brickMin - rayCast.origin) * invDir, (brickMax - rayCast.origin) * invDir);
                float exitT = min(min(bt1.x, bt1.y), bt1.z) + 1e-3;
                vec3 exitPoint = rayCast.origin + rayCast.dir * exitT;

                if (bt1.x <= bt1.y && bt1.x <= bt1.z)
                {
                    currentStep.x = step.x > 0.0 ? brickMax.x : brickMin.x - 1.0;
                    currentStep.y = floor(exitPoint.y);
                    currentStep.z = floor(exitPoint.z);
                    normal = vec3(-step.x, 0, 0);
                }
                else if (bt1.y <= bt1.z)
                {
                    currentStep.y = step.y > 0.0 ? brickMax.y : brickMin.y - 1.0;
                    currentStep.x = floor(exitPoint.x);
                    currentStep.z = floor(exitPoint.z);
                    normal = vec3(0, -step.y, 0);
                }
                else
                {
                    currentStep.z = step.z > 0.0 ? brickMax.z : brickMin.z - 1.0;
                    currentStep.x = floor(exitPoint.x);
                    currentStep.y = floor(exitPoint.y);
                    normal = vec3(0, 0, -step.z);
                }

                hitT = exitT;
                vec3 planes2 = currentStep + max(step, 0.0);
                t = (planes2 - rayCast.origin) * invDir;

                if (bt1.x <= bt1.y && bt1.x <= bt1.z) normal = vec3(-step.x, 0, 0);
                else if (bt1.y <= bt1.z)              normal = vec3(0, -step.y, 0);
                else                                  normal = vec3(0, 0, -step.z);
            }
            else
            {
                vec3 gridPos = rayCast.origin + rayCast.dir * hitT;
                voxelPos = gridPos * voxelSize;
                voxelCoord = ivec3(currentStep);

                ivec3 tangentU, tangentV;
                faceBasis(normal, tangentU, tangentV);
                vec3 local = gridPos - currentStep;
                uv = vec2(dot(local, vec3(tangentU)), dot(local, vec3(tangentV)));

                int voxelMat = voxels[flatten(ivec3(currentStep))];
                if (voxelMat != 0) return voxelMat;

                if (t.x < t.y && t.x < t.z) { hitT = t.x; currentStep.x += step.x; t.x += tDelta.x; normal = vec3(-step.x,0,0); }
                else if (t.y < t.z)         { hitT = t.y; currentStep.y += step.y; t.y += tDelta.y; normal = vec3(0,-step.y,0); }
                else                        { hitT = t.z; currentStep.z += step.z; t.z += tDelta.z; normal = vec3(0,0,-step.z); }
            }
        }

        if (currentStep.x >= width || currentStep.y >= height || currentStep.z >= depth) return 0;
        if (currentStep.x < 0 || currentStep.y < 0 || currentStep.z < 0) return 0;
    }
    return 0;
}

float ambientOcclusion(ivec3 voxelCoord, vec3 normal, ivec3 tangentU, ivec3 tangentV, int u, int v)
{
    ivec3 faceCenter = voxelCoord + ivec3(normal);
    ivec3 du = tangentU * (u == 0 ? -1 : 1);
    ivec3 dv = tangentV * (v == 0 ? -1 : 1);

    bool side1  = isSolid(faceCenter + du);
    bool side2  = isSolid(faceCenter + dv);
    bool corner = isSolid(faceCenter + du + dv);

    if (side1 && side2) return 0.0;
    return 3.0 - float(side1) - float(side2) - float(corner);
}

void main()
{
    float aspect = res.x / res.y;
    vec2 uv = fragPos * vec2(aspect, 1.0);
    vec3 rayDir = normalize(camDir + uv.x * camRight + uv.y * camUp);

    vec3 normal;
    vec3 hitPos;
    ivec3 voxelPos;
    vec2 voxelUV;
    int mat = voxelTraversal(Ray(camPos, rayDir), normal, hitPos, voxelUV, voxelPos);

    float radius = 1;
    float dMaxDistance = 30.0;
    float bMinDistance = 5.0;

    if (mouseInput != 0 && length(uv) < radius)
    {
        vec3 hitPoint;
        vec3 lastHitPoint;
        int lookedAt = voxelRaycast(Ray(camPos + (uv.x * camRight + uv.y * camUp), camDir), hitPoint, lastHitPoint);
        if (lookedAt != -1 && mouseInput == 1 && length(hitPoint - camPos) < dMaxDistance)
        {
            voxels[lookedAt] = 0;
            atomicAdd(brickCounts[brickFlatten(ivec3(hitPoint) / BRICK)], -1);
        }
        if (lookedAt != -1 && mouseInput == -1 && length(hitPoint - camPos) > bMinDistance && inBounds(floor(lastHitPoint)))
        {
            voxels[flatten(ivec3(lastHitPoint))] = buildMat;
            atomicAdd(brickCounts[brickFlatten(ivec3(lastHitPoint) / BRICK)], 1);
        }
    }

    if (mat == 0) FragColor = vec4(0.1, 0.12, 0.1, 1);
    else
    {
	    float ampientStrength = 0.2;
        float shadow = 1;

        if (dot(normal, -lightDir) >= 0.0)
        {
            vec3 dum1;
            vec3 dum2;
            int voxelLightHit = voxelRaycast(Ray(hitPos + normal * 0.4, -lightDir), dum1, dum2);
            shadow = voxelLightHit == -1 ? 1 : 0.2;
        }

        ivec3 tangentU, tangentV;
        faceBasis(normal, tangentU, tangentV);

        float ao00 = ambientOcclusion(voxelPos, normal, tangentU, tangentV, 0, 0);
        float ao10 = ambientOcclusion(voxelPos, normal, tangentU, tangentV, 1, 0);
        float ao01 = ambientOcclusion(voxelPos, normal, tangentU, tangentV, 0, 1);
        float ao11 = ambientOcclusion(voxelPos, normal, tangentU, tangentV, 1, 1);

        float aoBottom = mix(ao00, ao10, voxelUV.x);
        float aoTop    = mix(ao01, ao11, voxelUV.x);
        float ao       = mix(aoBottom, aoTop, voxelUV.y) / 3.0;

	    vec3 viewDir    = normalize(camPos - hitPos);
	    vec3 reflectDir = reflect(normalize(-lightDir), normalize(normal));

	    vec3 diffuse = lightColor * max(dot(normalize(normal), normalize(-lightDir)), 0.0) * shadow;
	    vec3 ambient = ampientStrength * lightColor;

        FragColor = vec4(materials[mat - 1] * (diffuse + ambient) * ao, 1);
    }
}
