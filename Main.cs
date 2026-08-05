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

        double time;

        Vector3 camPos = new Vector3(-3.0f, 0.0f, -3.0f);
        Vector3 camDir = Vector3.UnitZ;
        Vector3 camRight;
        Vector3 camUp;
        float yaw   = -MathHelper.PiOver2;
        float pitch = 0;

        public Main(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = (width, height), Title = title })
        {
        }
        Vector3 voxelRes = new Vector3(20, 20, 20);
        int[] voxels = new int[0];

        //int VertexBufferObject;
        //int VertexArrayObject;
        //Shader shader;

        int lampVAO;
        Shader lampShader;
        Vector3 lampPos = new Vector3(3.0f, -7.0f, 0.0f);

        Vector3 sunDir = new Vector3(0.7f, -1.0f, 0.5f);

        int fullScreenVAO;
        int fullScreenVBO;
        int fullScreenEBO;
        int voxelSSBO;
        Shader fullscreenShader;

        struct Ray(Vector3 origon, Vector3 dir)
        {
            public Vector3 origon = origon;
            public Vector3 dir = dir;
        }
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            camRight = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, camDir));
            camUp = Vector3.Normalize(Vector3.Cross(camDir, camRight));

            if (!IsFocused) return;

            float dt = (float)e.Time;

            float movementSpeed = 5.0f;
            float fastMovementSpeed = 30.0f;
            float sensitivity = 2f;

            Vector2 center = new Vector2(Size.X / 2, Size.Y / 2);

            float x = input(Keys.D, Keys.A) * dt;
            float y = input(Keys.E, Keys.Q) * dt;
            float z = input(Keys.W, Keys.S) * dt;
            float speed = KeyboardState.IsKeyDown(Keys.LeftShift) ? fastMovementSpeed : movementSpeed;
            camPos += (x * camRight + y * camUp + z * camDir) * speed;

            Vector2 mouseDelta = MousePosition - center;

            yaw   -= mouseDelta.X * sensitivity * dt;
            pitch -= mouseDelta.Y * sensitivity * dt;
            pitch = MathHelper.Clamp(pitch, MathHelper.DegreesToRadians(-89f), MathHelper.DegreesToRadians(89f));

            camDir = polarCoords(pitch, yaw);

            MousePosition = center;
            CursorState = CursorState.Hidden;
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

            voxels = new int[(int)(voxelRes.X * voxelRes.Y * voxelRes.Z)];
            for (int i = 0; i < voxelRes.X * voxelRes.Y * voxelRes.Z; i++)
            {
                voxels[i] = new Random().Next(4);
            }
            voxelSSBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, voxelSSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, voxels.Length * sizeof(int), voxels, BufferUsageHint.StaticDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, voxelSSBO);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            //VertexBufferObject = GL.GenBuffer();
            //GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            //GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            //VertexArrayObject = GL.GenVertexArray();
            //GL.BindVertexArray(VertexArrayObject);
            //GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            //GL.EnableVertexAttribArray(0);
            //GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            //GL.EnableVertexAttribArray(1);

            //shader = new Shader("inputs/shader.vert", "inputs/shader.frag");
            //shader.Use();

        }
        protected override void OnUnload()
        {
            base.OnUnload();

            //shader.Dispose();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            //Matrix4 view = Matrix4.LookAt(camPos, camPos + camDir, camUp);
            //Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(float.Pi * 0.5f, (float)Size.X / Size.Y, 0.1f, 100f);

            //GL.BindVertexArray(VertexArrayObject);
            //shader.Use();
            //shader.setVector3("lightColor", new Vector3(1.0f, 1.0f, 1.0f));
            //shader.setVector3("lightDir", sunDir);
            //shader.setVector3("camPos", camPos);
            //shader.setMatrix4("view", view);
            //shader.setMatrix4("projection", projection);
            //for (int x = 0; x < voxelRes.X; x++)
            //{
            //    for (int y = 0; y < voxelRes.Y; y++)
            //    {
            //        for (int z = 0; z < voxelRes.Z; z++)
            //        {
            //            int index = (int)(z * (voxelRes.X * voxelRes.Y) + y * voxelRes.X + x);
            //            if (voxels[index] == 0) continue;

            //            Matrix4 model = Matrix4.CreateTranslation(new Vector3(x, y, z));
            //            shader.setMatrix4("model", model);

            //            shader.setVector3("myMat.objectColor", materials[voxels[index] - 1].color);
            //            shader.setFloat("myMat.specularStrength", materials[voxels[index] - 1].shininess);

            //            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            //        }
            //    }
            //}

            fullscreenShader.Use();
            fullscreenShader.setVector3("camPos", camPos);
            fullscreenShader.setVector3("camDir", camDir);
            fullscreenShader.setVector3("camUp", camUp);
            fullscreenShader.setVector3("camRight", camRight);
            fullscreenShader.setVector3("lightColor", new Vector3(1.0f, 1.0f, 1.0f));
            fullscreenShader.setVector3("lightDir", sunDir);
            fullscreenShader.setVector3("voxelRes", voxelRes);
            fullscreenShader.setVector2("res", new Vector2(Size.X, Size.Y));

            GL.BindVertexArray(fullScreenVAO);
            GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedInt, 0);

            time += e.Time;

            if (time * 0.5f % 0.2f < 0.01f) Title = $"QuickRenderer | frameRate: {Math.Round(1 / e.Time)} | (:";

            SwapBuffers();
        }
        
        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            base.OnFramebufferResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
        }
    }
}
