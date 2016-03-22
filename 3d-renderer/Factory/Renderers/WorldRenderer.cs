using Factory.Physics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Factory.Renderers
{
    class WorldRenderer : IObjectRenderer
    {
        public ColladaGeometry AttachedObject { get; set; }
        public int DisplayListID { get; set; }
        protected WorldRenderer()
        {
        }
        public static WorldRenderer Create(ColladaGeometry World)
        {
            var WorldRender = new WorldRenderer();

            WorldRender.AttachedObject = World;
            return WorldRender;
        }
        public void ShareDisplayList(int otherDisplayListID)
        {
            DisplayListID = otherDisplayListID;
        }

        public int BuildDisplayList(bool drawDebug = false)
        {
            int list = 0;
            list = GL.GenLists(1);
            GL.NewList(list, ListMode.Compile);
            //DEBUG LOGGING
            DebugTools.Log("[Engine]: Building geometry into display list.");
            drawWorldPoly();
            if (drawDebug)
            {
                //DEBUG LOGGING
                DebugTools.Log("[Engine]: Building wireframe into display list.");
                drawWorldWireframe();
                //DEBUG LOGGING
                DebugTools.Log("[Engine]: Building normals into display list.");
                drawWorldNormals();
            }
            //DEBUG LOGGING
            DebugTools.Log("[Engine]: Building lines into display list.");
            drawWorldLines();

            //DEBUG LOGGING
            DebugTools.Log("[Engine]: Compiling display list.");
            GL.EndList();

            DisplayListID = list;

            return list;
        }

        public void AddToPhysics(BulletPhysics physicsWorld)
        {
            physicsWorld.AddStaticGeometry(AttachedObject, Matrix4.CreateTranslation(Vector3.Zero), this);

        }

        #region Static World Build List
        protected void drawWorldPoly()
        {
            OpenTK.Graphics.Color4 currentColor = new OpenTK.Graphics.Color4(100, 100, 100, 100);
            GL.PolygonOffset(1, 3);


            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Begin(PrimitiveType.Triangles);
            foreach (Triangle tri in AttachedObject.triangles)
            {
                if (currentColor != tri.color)
                {
                    // GL.Color4(currentColor);
                    GL.Color4(tri.color);
                    currentColor = tri.color;
                }
                GL.Normal3(tri.normals[0]);
                GL.Vertex3(tri.vertices[0]);
                GL.Normal3(tri.normals[1]);
                GL.Vertex3(tri.vertices[1]);
                GL.Normal3(tri.normals[2]);
                GL.Vertex3(tri.vertices[2]);

            }
            GL.End();
        }
        protected void drawWorldWireframe()
        {

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Color4(Color.Green);
            GL.Begin(PrimitiveType.Triangles);
            foreach (Triangle tri in AttachedObject.triangles)
            {
                GL.Normal3(tri.normals[0]);
                GL.Vertex3(tri.vertices[0]);
                GL.Normal3(tri.normals[1]);
                GL.Vertex3(tri.vertices[1]);
                GL.Normal3(tri.normals[2]);
                GL.Vertex3(tri.vertices[2]);
            }
            GL.End();
        }

        protected void drawWorldNormals()
        {


            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.ColorMaterial);


            GL.Begin(PrimitiveType.Lines);
            GL.Color3(Color.Red);
            foreach (Triangle tri in AttachedObject.triangles)
            {
                GL.Vertex3(tri.vertices[0]);
                GL.Vertex3(tri.vertices[0] + tri.normals[0] * 4);
                GL.Vertex3(tri.vertices[1]);
                GL.Vertex3(tri.vertices[1] + tri.normals[1] * 4);
                GL.Vertex3(tri.vertices[2]);
                GL.Vertex3(tri.vertices[2] + tri.normals[2] * 4);
            }

            GL.End();

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.ColorMaterial);
        }

        protected void drawWorldLines()
        {
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.ColorMaterial);

            GL.Begin(PrimitiveType.Lines);
            GL.Color3(Color.DarkBlue);
            foreach (GeometryLine line in AttachedObject.geometryLines)
            {
                GL.Vertex3(line.vertices[0]);
                GL.Vertex3(line.vertices[1]);
            }

            GL.End();

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.ColorMaterial);
        }
        #endregion

        public void Render()
        {
            GL.CallList(DisplayListID);

        }
    }
}
