using System;

using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace VoxelRenderer
{
    public class Main : GameWindow
    {
        Shader shader;

        Texture tex1;
        Texture tex2;

        double time;

        Vector3 camPos = new Vector3(0.0f, 0.0f, -3.0f);
        Vector3 camDir = Vector3.UnitZ;
        Vector3 camRight;
        Vector3 camUp;

        public Main(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = (width, height), Title = title })
        {
        }

        float[] vertices = 
        {
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,

            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,

            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,

            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f
        };

        uint[] indices = {
            0, 1, 2,   // first triangle
            0, 2, 3,   // second triangle
        };

        int VertexBufferObject;
        int VertexArrayObject;
        int ElementBufferObject;
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!IsFocused) return;

            float dt = (float)e.Time;

            float movementSpeed = 5;
            float sensitivity = 2;

            Vector2 center = new Vector2(Size.X / 2, Size.Y / 2);
            camRight = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, camDir));
            camUp = Vector3.Normalize(Vector3.Cross(camDir, camRight));

            float x = input(Keys.A, Keys.D) * dt;
            float y = input(Keys.E, Keys.Q) * dt;
            float z = input(Keys.W, Keys.S) * dt;
            camPos += (x * camRight + y * camUp + z * camDir) * movementSpeed;

            Vector2 mouseDelta = MousePosition - center;
            camDir *= Matrix3.CreateRotationY(-mouseDelta.X * dt * sensitivity);

            MousePosition = center;
            CursorState = CursorState.Hidden;
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

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            ElementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            shader = new Shader("\\projectCof\\VoxelRenderer\\VoxelRenderer\\inputs\\shader.vert", "\\projectCof\\VoxelRenderer\\VoxelRenderer\\inputs\\shader.frag");
            shader.Use();

            tex1 = new Texture("\\projectCof\\VoxelRenderer\\VoxelRenderer\\inputs\\catTouching.jpg");
            tex2 = new Texture("\\projectCof\\VoxelRenderer\\VoxelRenderer\\inputs\\catStanding.png");

            shader.SetInt("texture1", 1);
            shader.SetInt("texture2", 2);
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
            GL.Enable(EnableCap.DepthTest);

            tex1.Use(TextureUnit.Texture1);
            tex2.Use(TextureUnit.Texture2);
            shader.Use();

            GL.BindVertexArray(VertexArrayObject);
            //GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            time += e.Time;

            Matrix4 model = Matrix4.CreateRotationY((float)time) * Matrix4.CreateRotationX((float)time);
            Matrix4 view = Matrix4.LookAt(camPos, camPos + camDir, camUp);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(float.Pi * 0.25f, (float)Size.X / Size.Y, 0.1f, 100f);

            GL.UseProgram(shader.Handle);
            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);

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
