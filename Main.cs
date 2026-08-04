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

        Vector3 camPos = new Vector3(0.0f, 0.0f, 3.0f);
        Vector3 camDir = Vector3.UnitZ;
        Vector3 camRight;
        Vector3 camUp;
        float yaw   = -MathHelper.PiOver2;
        float pitch = 0;

        public Main(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = (width, height), Title = title })
        {
        }
        float[] vertices =
        {
            // Back face (normal: 0, 0, -1)
            -0.5f, -0.5f, -0.5f,   0.0f,  0.0f, -1.0f,
             0.5f, -0.5f, -0.5f,   0.0f,  0.0f, -1.0f,
             0.5f,  0.5f, -0.5f,   0.0f,  0.0f, -1.0f,
             0.5f,  0.5f, -0.5f,   0.0f,  0.0f, -1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f,  0.0f, -1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f,  0.0f, -1.0f,

            // Front face (normal: 0, 0, 1)
            -0.5f, -0.5f,  0.5f,   0.0f,  0.0f,  1.0f,
             0.5f, -0.5f,  0.5f,   0.0f,  0.0f,  1.0f,
             0.5f,  0.5f,  0.5f,   0.0f,  0.0f,  1.0f,
             0.5f,  0.5f,  0.5f,   0.0f,  0.0f,  1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f,  0.0f,  1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f,  0.0f,  1.0f,

            // Left face (normal: -1, 0, 0)
            -0.5f,  0.5f,  0.5f,  -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f, -0.5f,  -1.0f,  0.0f,  0.0f,
            -0.5f, -0.5f, -0.5f,  -1.0f,  0.0f,  0.0f,
            -0.5f, -0.5f, -0.5f,  -1.0f,  0.0f,  0.0f,
            -0.5f, -0.5f,  0.5f,  -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,  -1.0f,  0.0f,  0.0f,

            // Right face (normal: 1, 0, 0)
             0.5f,  0.5f,  0.5f,   1.0f,  0.0f,  0.0f,
             0.5f,  0.5f, -0.5f,   1.0f,  0.0f,  0.0f,
             0.5f, -0.5f, -0.5f,   1.0f,  0.0f,  0.0f,
             0.5f, -0.5f, -0.5f,   1.0f,  0.0f,  0.0f,
             0.5f, -0.5f,  0.5f,   1.0f,  0.0f,  0.0f,
             0.5f,  0.5f,  0.5f,   1.0f,  0.0f,  0.0f,

            // Bottom face (normal: 0, -1, 0)
            -0.5f, -0.5f, -0.5f,   0.0f, -1.0f,  0.0f,
             0.5f, -0.5f, -0.5f,   0.0f, -1.0f,  0.0f,
             0.5f, -0.5f,  0.5f,   0.0f, -1.0f,  0.0f,
             0.5f, -0.5f,  0.5f,   0.0f, -1.0f,  0.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, -1.0f,  0.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, -1.0f,  0.0f,

            // Top face (normal: 0, 1, 0)
            -0.5f,  0.5f, -0.5f,   0.0f,  1.0f,  0.0f,
             0.5f,  0.5f, -0.5f,   0.0f,  1.0f,  0.0f,
             0.5f,  0.5f,  0.5f,   0.0f,  1.0f,  0.0f,
             0.5f,  0.5f,  0.5f,   0.0f,  1.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,   0.0f,  1.0f,  0.0f,
            -0.5f,  0.5f, -0.5f,   0.0f,  1.0f,  0.0f
        };

        Vector3 voxelRes = new Vector3(5, 5, 5);
        int[] voxels =
        {
            //layer 0
            3, 0, 1,
            0, 2, 0,
            2, 0, 3,

            //layer 1
            2, 0, 3,
            0, 1, 0,
            2, 0, 1,

            //layer 2
            1, 0, 3,
            0, 2, 0,
            3, 0, 1,
        };

        struct Material(Vector3 color, float shininess)
        {
            public Vector3 color = color;
            public float shininess = shininess;
        }
        Material[] materials =
        {
            new Material(new Vector3(0.1f, 0.05f, 0.2f), 0.2f),
            new Material(new Vector3(0.8f, 0.5f, 0.05f), 0.8f),
            new Material(new Vector3(0.1f, 0.30f, 0.8f), 1.0f)
        };

        int VertexBufferObject;
        int VertexArrayObject;
        Shader shader;

        int lampVAO;
        Shader lampShader;
        Vector3 lampPos = new Vector3(3.0f, -7.0f, 0.0f);

        Vector3 sunDir = new Vector3(0.7f, 1.0f, 0.5f);
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!IsFocused) return;

            float dt = (float)e.Time;

            float movementSpeed = 5;
            float sensitivity = 2f;

            Vector2 center = new Vector2(Size.X / 2, Size.Y / 2);
            camRight = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, camDir));
            camUp = Vector3.Normalize(Vector3.Cross(camDir, camRight));

            float x = input(Keys.A, Keys.D) * dt;
            float y = input(Keys.E, Keys.Q) * dt;
            float z = input(Keys.W, Keys.S) * dt;
            camPos += (x * camRight + y * camUp + z * camDir) * movementSpeed;

            Vector2 mouseDelta = MousePosition - center;

            yaw   += mouseDelta.X * sensitivity * dt;
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

            GL.ClearColor(0.3f, 0.4f, 0.4f, 1.0f);

            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            shader = new Shader("inputs/shader.vert", "inputs/shader.frag");
            shader.Use();

            voxels = new int[(int)(voxelRes.X * voxelRes.Y * voxelRes.Z)];
            for (int i = 0; i < voxelRes.X * voxelRes.Y * voxelRes.Z; i++)
            {
                voxels[i] = new Random().Next(materials.Length + 1);
            }

            GL.Enable(EnableCap.DepthTest);
        }
        protected override void OnUnload()
        {
            base.OnUnload();

            shader.Dispose();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 view = Matrix4.LookAt(camPos, camPos + camDir, camUp);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(float.Pi * 0.5f, (float)Size.X / Size.Y, 0.1f, 100f);

            GL.BindVertexArray(VertexArrayObject);
            shader.Use();
            shader.setVector3("lightColor", new Vector3(1.0f, 1.0f, 1.0f));
            shader.setVector3("lightDir", sunDir);
            shader.setVector3("camPos", camPos);
            shader.setMatrix4("view", view);
            shader.setMatrix4("projection", projection);
            for (int x = 0; x < voxelRes.X; x++)
            {
                for (int y = 0; y < voxelRes.Y; y++)
                {
                    for (int z = 0; z < voxelRes.Z; z++)
                    {
                        int index = (int)(z * (voxelRes.X * voxelRes.Y) + y * voxelRes.X + x);
                        if (voxels[index] == 0) continue;

                        Matrix4 model = Matrix4.CreateTranslation(new Vector3(x, y, z));
                        shader.setMatrix4("model", model);

                        shader.setVector3("myMat.objectColor", materials[voxels[index] - 1].color);
                        shader.setFloat("myMat.specularStrength", materials[voxels[index] - 1].shininess);

                        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
                    }
                }
            }


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
