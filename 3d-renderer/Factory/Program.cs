using ColladaDotNet;
using Factory.AI;
using Factory.Physics;
using Factory.Renderers;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Factory
{
    class Program
    {
        static void Main(string[] args)
        {
            RenderWindow window = new RenderWindow();
            window.VSync = VSyncMode.Adaptive;
            window.Title = Constants.name + " " + Constants.version + " " + Constants.type;

            window.Run(60, 60);
        }
    }

    class RenderWindow : GameWindow
    {
        BulletPhysics physics_world = new BulletPhysics();

        Vector3 cameraLocation = new Vector3(-98.5f, 366.75f, 93.724f);
        Vector3 rtLocation = new Vector3(0.4771588f, -0.8788171f, -0.2693401f);
        Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.Zero, Vector3.UnitZ);
        public RenderWindow()
            : base(1000, 600, new GraphicsMode(32, 24, 0, 4), "")
        {

        }

        PhysicsObjectRenderer Box;
        WorldRenderer world;
        int[] icons = { 0, 0, 0, 0 };
        WaypointManager wm = new WaypointManager();
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            world = WorldRenderer.Create(ColladaGeometry.LoadDAE("Assets/room.dae"));
            Box = PhysicsObjectRenderer.Create(ColladaGeometry.LoadDAE("Assets/box.dae"));

            Box.BuildDisplayList();

            InitializeOpenGL();

            Mouse.ButtonDown += MouseClick;
            //build the world
            world.BuildDisplayList();
            world.AddToPhysics(physics_world);
            icons[0] = ImageLoader.LoadTexture("Assets/InterfaceTextures/AI Spawn.png");
            icons[1] = ImageLoader.LoadTexture("Assets/InterfaceTextures/Camera.png");
            icons[2] = ImageLoader.LoadTexture("Assets/InterfaceTextures/Physics Spawn.png");
            icons[3] = ImageLoader.LoadTexture("Assets/InterfaceTextures/Waypoint Logo.png");

        }

        protected void InitializeOpenGL()
        {

            //OpenGL Initialization
            GL.ClearColor(Color.Black);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.LineSmooth);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.ColorMaterial);
            GL.Enable(EnableCap.Light0);
            //GL.Enable(EnableCap.Normalize);
            GL.Light(LightName.Light0, LightParameter.Diffuse, new float[] { .8f, .8f, .8f, 1 });
            GL.Light(LightName.Light0, LightParameter.Ambient, new float[] { .2f, .2f, .2f, 1 });
            GL.Light(LightName.Light0, LightParameter.SpotCutoff, 180F);
            GL.Light(LightName.Light0, LightParameter.SpotExponent, 5f);
            GL.Light(LightName.Light0, LightParameter.QuadraticAttenuation, .33f);
            GL.LineWidth(1.5f);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            GL.LoadIdentity();
        }

        protected void drawInfoDialog(Vector3 Location, int texture)
        {
            GL.Enable(EnableCap.Texture2D);
            GL.Begin(PrimitiveType.Quads);
            GL.BindTexture(TextureTarget.Texture2DMultisample, texture);
            GL.Color4(Color.White);
            GL.TexCoord2(1, 1);
            GL.Vertex3(new Vector3(-6, 0, -6) + Location);
            GL.TexCoord2(0, 1);
            GL.Vertex3(new Vector3(6, 0, -6) + Location);
            GL.TexCoord2(0, 0);
            GL.Vertex3(new Vector3(6, 0, 6) + Location);
            GL.TexCoord2(1, 0);
            GL.Vertex3(new Vector3(-6, 0, 6) + Location);
            GL.End();
            GL.Disable(EnableCap.Texture2D);

        }
        protected void drawLine(Vector3 line1, Vector3 line2, Color4 Color)
        {
            GL.Begin(PrimitiveType.Lines);
            GL.Color4(Color);
            GL.Vertex3(line1);
            GL.Vertex3(line2);
            GL.End();
        }
        protected void drawPhysicsObjects()
        {

            Matrix4 mview = new Matrix4(modelview.Row0, modelview.Row1, modelview.Row2, modelview.Row3);
            foreach (BulletSharp.RigidBody body in physics_world.physics_world.CollisionObjectArray)
            {
                if (!body.IsStaticObject)
                {
                    Matrix4 modelLookAt = body.MotionState.WorldTransform * mview;
                    GL.LoadMatrix(ref modelLookAt);
                    ((PhysicsObjectRenderer)body.CollisionShape.UserObject).Render();
                }

            }

            GL.LoadMatrix(ref mview); //Restore the old view matrix
        }

        public double[] lastFrames = { 16.66666666666, 16.6666666666666, 16.66666666666, 16.6666666666666, 16.666666666666 };
        public DateTime lastFrame = DateTime.Now;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.ShadeModel(ShadingModel.Smooth);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            world.Render();
            drawPhysicsObjects();

            foreach (Waypoint w in wm.Waypoints.Values)
            {
                drawInfoDialog(w.Location, icons[3]);
                foreach (int Attached in w.AttachedWaypoints)
                {
                    drawLine(wm.GetWaypoint(Attached).Location, w.Location, Color.Red);
                }
            }
            //FPS Calculation
            lastFrames[0] = lastFrames[1];
            lastFrames[1] = lastFrames[2];
            lastFrames[2] = lastFrames[3];
            lastFrames[3] = lastFrames[4];
            lastFrames[4] = (DateTime.Now - lastFrame).TotalMilliseconds;
            lastFrame = DateTime.Now;

            SwapBuffers();

        }

        float movementSpeed = 0f;
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            UpdateMousePosition();
            this.Title =
                "Mode: " + wm.Waypoints.Count +
                " Physics Bodies: " + physics_world.physics_world.CollisionObjectArray.Count +
                " FPS: " + Math.Floor(1000 / lastFrames.Average()) +
                " Camera: X: " + cameraLocation.X.ToString("N1") +
                " Y: " + cameraLocation.Y.ToString("N1") +
                " Z: " + cameraLocation.Z.ToString("N1") +
                " Pointing: H: " + rotateOffset.X.ToString("N1") +
                " V: " + rotateOffset.Y.ToString("N1") +
                " X: " + rtLocation.X.ToString("N1") +
                " Y: " + rtLocation.Y.ToString("N1") +
                " Z: " + rtLocation.Z.ToString("N1");
            #region Change Movement Speed
            bool moving = false;
            float imovementSpeed = movementSpeed;
            if (OpenTK.Input.Keyboard.GetState(0).IsKeyDown(OpenTK.Input.Key.ShiftLeft))
            {
                imovementSpeed *= 5;
            }
            #endregion

            #region Perspective Warping
            UpdateZoom(imovementSpeed);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / zoomDivisor, Width / (float)Height, 1f, float.MaxValue);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            #endregion

            #region Input Handlers
            if (OpenTK.Input.Keyboard.GetState(0).IsKeyDown(OpenTK.Input.Key.A))
            {
                moving = true;
                cameraLocation.X -= (float)Math.Sin((Math.PI / 180) * rotateOffset.X + Math.PI / 2) * imovementSpeed;
                cameraLocation.Y -= (float)Math.Cos((Math.PI / 180) * rotateOffset.X + Math.PI / 2) * imovementSpeed;
            }
            if (OpenTK.Input.Keyboard.GetState(0).IsKeyDown(OpenTK.Input.Key.D))
            {
                moving = true;
                cameraLocation.X += (float)Math.Sin((Math.PI / 180) * rotateOffset.X + Math.PI / 2) * imovementSpeed;
                cameraLocation.Y += (float)Math.Cos((Math.PI / 180) * rotateOffset.X + Math.PI / 2) * imovementSpeed;
            }
            if (OpenTK.Input.Keyboard.GetState(0).IsKeyDown(OpenTK.Input.Key.W))
            {
                moving = true;
                float HypotenuseXY = (float)Math.Cos((Math.PI / 180) * rotateOffset.Y) * imovementSpeed;
                cameraLocation.X += HypotenuseXY * (float)Math.Sin((Math.PI / 180) * rotateOffset.X);
                cameraLocation.Y += HypotenuseXY * (float)Math.Cos((Math.PI / 180) * rotateOffset.X);
                cameraLocation.Z += (float)Math.Sin((Math.PI / 180) * rotateOffset.Y) * imovementSpeed;
            }
            if (OpenTK.Input.Keyboard.GetState(0).IsKeyDown(OpenTK.Input.Key.S))
            {
                moving = true;
                float HypotenuseXY = (float)Math.Cos((Math.PI / 180) * rotateOffset.Y) * imovementSpeed;
                cameraLocation.X -= HypotenuseXY * (float)Math.Sin((Math.PI / 180) * rotateOffset.X);
                cameraLocation.Y -= HypotenuseXY * (float)Math.Cos((Math.PI / 180) * rotateOffset.X);
                cameraLocation.Z -= (float)Math.Sin((Math.PI / 180) * rotateOffset.Y) * imovementSpeed;
            }
            if (OpenTK.Input.Keyboard.GetState(0).IsKeyDown(OpenTK.Input.Key.Z))
            {
                moving = true;
                cameraLocation.Z += imovementSpeed;
            }
            if (OpenTK.Input.Keyboard.GetState(0).IsKeyDown(OpenTK.Input.Key.X))
            {
                moving = true;
                cameraLocation.Z -= imovementSpeed;
            }
            #endregion

            #region Physics Engine Step
            if (OpenTK.Input.Keyboard.GetState(0).IsKeyDown(OpenTK.Input.Key.LControl))
            {
                physics_world.Step(0.0001f);
            }
            else
            {
                physics_world.Step((float)lastFrames.Average() / 1000);
            }
            #endregion
            UpdateMoveSpeed(moving);
            modelview = Matrix4.LookAt(cameraLocation, cameraLocation + rtLocation, Vector3.UnitZ);
            GL.MatrixMode(MatrixMode.Modelview);

            GL.LoadMatrix(ref modelview);

            //and move the camera to the light

            float[] lightLocation = { cameraLocation.X, cameraLocation.Y, cameraLocation.Z };
            GL.Light(LightName.Light0, LightParameter.Position, lightLocation);
        }
        float zoomDivisor = 4;
        void UpdateZoom(float moveSpeed)
        {
            zoomDivisor = 4;
        }
        void UpdateMoveSpeed(bool keypressed = true)
        {
            if (movementSpeed < 1.5f && keypressed)
            {
                movementSpeed += 0.1f;
            }
            if (movementSpeed > 0f && !keypressed)
            {
                movementSpeed = 0f;
            }
        }

        int mode = 3;
        protected override void OnKeyDown(OpenTK.Input.KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == OpenTK.Input.Key.Q)
            {
                mode++;
                if (mode > 3)
                {
                    mode = 0;
                }
            }
            if (e.Key == OpenTK.Input.Key.Tab)
            {
                wm.AddWaypoint(cameraLocation + (rtLocation * 6));
            }

            if (e.Key == OpenTK.Input.Key.Tilde)
            {
                //Add a box to the scene
                physics_world.AddDynamicGeometry(
                    Box.AttachedObject,
                    Matrix4.CreateTranslation(cameraLocation),
                    Box);
            }
            if (e.Key == OpenTK.Input.Key.Escape)
            {
                this.Exit();
                Application.Exit();
            }
            if (e.Alt && e.Key == OpenTK.Input.Key.Enter)
            {
                if (!(this.WindowState == WindowState.Fullscreen))
                {
                    this.WindowBorder = WindowBorder.Hidden;
                    this.WindowState = WindowState.Fullscreen;
                }
                else
                {
                    this.WindowBorder = WindowBorder.Resizable;
                    this.WindowState = WindowState.Normal;
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //Close the physics
            physics_world.Dispose();
            base.OnClosing(e);
        }
        public PointF rotateOffset = new PointF(145.5f, -17.875f);
        Point pointer_current, pointer_previous;
        Size pointer_delta;
        void UpdateMousePosition()
        {
            if (mouseLock)
            {
                //Hide Cursor
                System.Windows.Forms.Cursor.Hide();
                //Calculate delta
                pointer_previous = new Point(Bounds.Left + (Bounds.Width / 2), Bounds.Top + (Bounds.Height / 2));
                pointer_current = System.Windows.Forms.Cursor.Position;
                pointer_delta = new Size(pointer_current.X - pointer_previous.X,
                    pointer_current.Y - pointer_previous.Y);
                //and reset location
                System.Windows.Forms.Cursor.Position =
                    new Point(Bounds.Left + (Bounds.Width / 2), Bounds.Top + (Bounds.Height / 2));
                //and calculate the angle
                rotateOffset.X += (float)pointer_delta.Width / 8;
                if (rotateOffset.X < 0)
                {
                    rotateOffset.X = 360F;
                }
                if (rotateOffset.X > 360)
                {
                    rotateOffset.X = 0;
                }
                rotateOffset.Y -= (float)pointer_delta.Height / 8;
                if (rotateOffset.Y > 89.99F)
                {
                    rotateOffset.Y = 89.99F;
                }
                if (rotateOffset.Y < -89.99F)
                {
                    rotateOffset.Y = -89.99F;
                }
                float HypotenuseXY = (float)Math.Cos((Math.PI / 180) * rotateOffset.Y);
                rtLocation.X = HypotenuseXY * (float)Math.Sin((Math.PI / 180) * rotateOffset.X);
                rtLocation.Y = HypotenuseXY * (float)Math.Cos((Math.PI / 180) * rotateOffset.X);
                rtLocation.Z = (float)Math.Sin((Math.PI / 180) * rotateOffset.Y);
            }
        }
        bool mouseLock = false;
        protected void MouseClick(object s, OpenTK.Input.MouseButtonEventArgs e)
        {
            if (e.Button == OpenTK.Input.MouseButton.Left)
            {
                mouseLock = !mouseLock;
                if (mouseLock)
                {
                    System.Windows.Forms.Cursor.Hide();
                    System.Windows.Forms.Cursor.Position =
                        new Point(Bounds.Left + (Bounds.Width / 2), Bounds.Top + (Bounds.Height / 2));
                }
                else
                {
                    System.Windows.Forms.Cursor.Show();
                }
            }
        }
        protected override void OnResize(EventArgs e)
        {

            base.OnResize(e);

            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1f, float.MaxValue);

            GL.MatrixMode(MatrixMode.Projection);

            GL.LoadMatrix(ref projection);


            modelview = Matrix4.LookAt(cameraLocation, cameraLocation + rtLocation, Vector3.UnitZ);

            GL.MatrixMode(MatrixMode.Modelview);

            GL.LoadMatrix(ref modelview);
        }

    }
}
