using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace VoxelRenderer
{
    public class Main : GameWindow
    {
        float stepTimer;

        Vector3 camPos = new Vector3(-3.0f, 0.0f, -3.0f);
        Vector3 camDir = Vector3.UnitZ;
        Vector3 camRight;
        Vector3 camUp;
        float yaw   = -MathHelper.PiOver2;
        float pitch = 0;

        public Main(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = (width, height), Title = title })
        {
        }
        Vector3i voxelRes = new Vector3i(700, 400, 700);
        int[] voxels = new int[0];

        Vector3 sunDir = new Vector3(0.7f, -1.0f, 0.5f);

        int fullScreenVAO;
        int fullScreenVBO;
        int fullScreenEBO;
        int voxelSSBO;
        Shader fullscreenShader;
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            camRight = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, camDir));
            camUp = Vector3.Normalize(Vector3.Cross(camDir, camRight));

            if (!IsFocused) return;

            float dt = (float)e.Time;

            float movementSpeed = 5.0f;
            float fastMovementSpeed = 30.0f;
            float sensitivity = 0.007f;

            Vector2 center = new Vector2(Size.X / 2, Size.Y / 2);

            float x = input(Keys.D, Keys.A) * dt;
            float y = input(Keys.E, Keys.Q) * dt;
            float z = input(Keys.W, Keys.S) * dt;
            float speed = KeyboardState.IsKeyDown(Keys.LeftShift) ? fastMovementSpeed : movementSpeed;
            camPos += (x * camRight + y * camUp + z * camDir) * speed;

            Vector2 mouseDelta = MousePosition - center;

            yaw   -= mouseDelta.X * sensitivity;
            pitch -= mouseDelta.Y * sensitivity;
            pitch = MathHelper.Clamp(pitch, MathHelper.DegreesToRadians(-89f), MathHelper.DegreesToRadians(89f));

            camDir = polarCoords(pitch, yaw);

            MousePosition = center;
            CursorState = CursorState.Hidden;

            stepTimer -= (float)e.Time;
            fullscreenShader.setFloat("time", stepTimer);
            float timeToStep = 0.1f;
            if (stepTimer <= 0)
            {
                stepTimer += timeToStep;
                Title = $"QuickRenderer | frameRate: {Math.Round(1 / e.Time)} | (: | voxels grid: {voxelRes.X}x{voxelRes.Y}x{voxelRes.Z} | resolution: {Size.X}x{Size.Y}";
            }
        }

        Vector3 polarCoords(float pitch, float yaw)
        {
            float x = MathF.Cos(pitch) * MathF.Sin(yaw);
            float y = MathF.Sin(pitch);
            float z = -MathF.Cos(pitch) * MathF.Cos(yaw);
            return new Vector3(x, y, z).Normalized();
        }

        float input(Keys posKey, Keys negKey)
        {
            float value = 0;
            if (KeyboardState.IsKeyDown(posKey))
            {
                value += 1;
            }
            if (KeyboardState.IsKeyDown(negKey))
            {
                value -= 1;
            }
            return value;
        }
        float input(MouseButton posKey, MouseButton negKey)
        {
            float value = 0;
            if (MouseState.IsButtonDown(posKey))
            {
                value += 1;
            }
            if (MouseState.IsButtonDown(negKey))
            {
                value -= 1;
            }
            return value;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.1f, 0.1f, 0.11f, 1.0f);
            MousePosition = new Vector2(Size.X / 2, Size.Y / 2);

            float[] fullscreenVertices =
            {
                 1.0f,  1.0f, 0.0f,  // top right
                 1.0f, -1.0f, 0.0f,  // bottom right
                -1.0f, -1.0f, 0.0f,  // bottom left
                -1.0f,  1.0f, 0.0f   // top left
            };
            uint[] indices = {
                0, 1, 3,   // first triangle
                1, 2, 3    // second triangle
            };

            fullScreenVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, fullScreenVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, fullscreenVertices.Length * sizeof(float), fullscreenVertices, BufferUsageHint.StaticDraw);

            fullScreenVAO = GL.GenVertexArray();
            GL.BindVertexArray(fullScreenVAO);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            fullscreenShader = new Shader("inputs/voxelRayTracer.vert", "inputs/voxelRayTracer.frag");
            fullscreenShader.Use();

            fullScreenEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, fullScreenEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            long voxelsAmount = (long)voxelRes.X * voxelRes.Y * voxelRes.Z;
            if (voxelsAmount > int.MaxValue)
            {
                throw new Exception("bro, ts too large");
            }
            voxels = new int[voxelsAmount];
            FastNoiseLite noise = new FastNoiseLite(0);
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            noise.SetFractalType(FastNoiseLite.FractalType.FBm);
            noise.SetFractalLacunarity(2.0f);
            noise.SetFractalOctaves(3);
            noise.SetFrequency(0.004f);
            for (int x = 0; x < voxelRes.X; x++)
            {
                for (int z = 0; z < voxelRes.Z; z++)
                {
                    for (int y = 0; y < voxelRes.Y; y++)
                    {
                        float grassThreshold = 0.8f;
                        float dirtThreshold  = 0.77f;
                        float stoneThreshold = 0.65f;

                        float height = (float)y / voxelRes.Y;
                        float noiseValue = noise.GetNoise(x, z) * 0.5f + 0.5f;
                        int index = flatten(x, y, z);

                        voxels[index] = 0;
                        if      (height < stoneThreshold * noiseValue) voxels[index] = 3;
                        else if (height < dirtThreshold  * noiseValue) voxels[index] = 2;
                        else if (height < grassThreshold * noiseValue) voxels[index] = 1;
                    }
                }
            }
            voxelSSBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, voxelSSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, voxels.Length * sizeof(int), voxels, BufferUsageHint.StaticDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, voxelSSBO);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        int flatten(int x, int y, int z)
        {
            long index = z * (voxelRes.X * (long)voxelRes.Y) + (long)y * voxelRes.X + x;
            return (int)index;
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            fullscreenShader.Dispose();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            fullscreenShader.Use();
            fullscreenShader.setVector3("camPos", camPos);
            fullscreenShader.setVector3("camDir", camDir);
            fullscreenShader.setVector3("camUp", camUp);
            fullscreenShader.setVector3("camRight", camRight);
            fullscreenShader.setVector3("lightColor", new Vector3(1.0f, 1.0f, 1.0f));
            fullscreenShader.setVector3("lightDir", sunDir);
            fullscreenShader.setInt("width", voxelRes.X);
            fullscreenShader.setInt("height", voxelRes.Y);
            fullscreenShader.setInt("depth", voxelRes.Z);
            fullscreenShader.setInt("mouseInput", (int)input(MouseButton.Left, MouseButton.Right));
            fullscreenShader.setVector2("res", new Vector2(Size.X, Size.Y));

            GL.BindVertexArray(fullScreenVAO);
            GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
        }
        
        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            base.OnFramebufferResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
        }
    }
}
