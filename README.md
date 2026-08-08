# Quick Voxels
<img width="630" height="500" alt="quick voxels thumpnail" src="https://github.com/user-attachments/assets/6f727483-4783-4bb1-bb02-f4dd38b8dfb0" /> </br>
An optimized Voxel renderer with a lot of futures and optimization techniques written in [OpenTk](https://opentk.net)

## How to use it
it's prolly not the best to use but you can just copy the repo files if you want
### I want to check the performance
you can download an .exe from the releases or from [it's itch page](https://cof99.itch.io/quick-voxels) </br>
it runs on 300fps with 700x300x700 voxels
note: if you want to build use the numbers to select what material to use

## how does it work
unlike normal voxel renderers who construct a mesh from the voxel data and renders it using the gpu, mine casts a ray for each pixel using the gpu then if that ray hits something color the respective pixel if not just output the sky color </br>
this way of doing it allows things that can't be done normally. if you want to hear more about this topic check out [this video](https://www.youtube.com/watch?v=ztkh1r1ioZo&t=2s)
### futures + optimizations
shadows: for each ray hit, shoot yet another ray from the voxel to the sun if the ray hits something shade that part of the voxel
ambient occlusion: For each ray hit, count how many voxels are there in the 4 corners of the voxel face the more solid neighbors nearby the darker that part of the voxel
BRICKS: every traversing ray stops for every potential voxel if there's a huge part that contains no solid voxels the ray will still check for a voxel a lot. bricks are 16x16x16 areas that tells the ray: "wait bro there's no voxels in me go around me and don't check for anything"
procedural generation: uses [Perlin noise](https://en.wikipedia.org/wiki/Perlin_noise) generated using the tool [FastNoiseLite](https://github.com/Auburn/FastNoiseLite) to generate voxels with different colors.
