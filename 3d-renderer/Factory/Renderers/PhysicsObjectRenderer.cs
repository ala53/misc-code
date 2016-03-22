using BulletSharp;
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
    class PhysicsObjectRenderer : IObjectRenderer
    {
        public ColladaGeometry AttachedObject { get; set; }
        public int DisplayListID { get; set; }

        protected PhysicsObjectRenderer()
        {
        }

        public static PhysicsObjectRenderer Create(ColladaGeometry geometry)
        {
            var Renderer = new PhysicsObjectRenderer();
            Renderer.AttachedObject = geometry;
            return Renderer;
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

            drawTriangles(AttachedObject.triangles);
            drawGeometryLines(AttachedObject.geometryLines);
            drawDebugAABB = drawDebug;

            GL.EndList();
            DisplayListID = list;
            return list;

        }


        protected void drawTriangles(List<Triangle> tris)
        {
            OpenTK.Graphics.Color4 currentColor = new OpenTK.Graphics.Color4(100, 100, 100, 100);
            GL.PolygonOffset(1, 3);


            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Begin(PrimitiveType.Triangles);
            foreach (Triangle tri in tris)
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
        protected void drawGeometryLines(List<GeometryLine> lines)
        {

            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.ColorMaterial);

            GL.Begin(PrimitiveType.Lines);
            GL.Color3(Color.DarkBlue);
            foreach (GeometryLine line in lines)
            {
                GL.Vertex3(line.vertices[0]);
                GL.Vertex3(line.vertices[1]);
            }

            GL.End();

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.ColorMaterial);
        }

        bool drawDebugAABB = false;
        public void Render()
        {

            GL.CallList(DisplayListID);

        }
        public void DrawAABBDebug(RigidBody PhysicsBody)
        {
            Vector3 AABBMin = new Vector3();
            Vector3 AABBMax = new Vector3();
            PhysicsBody.GetAabb(out AABBMin, out AABBMax);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Begin(PrimitiveType.Quads);
            GL.Color4(1, 0, 0, 0.2f);
            GL.Vertex3(AABBMin);
            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));


            GL.Vertex3(AABBMin);
            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, 0));


            GL.Vertex3(AABBMin);
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, 0));


            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, 0));
            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z) + new Vector3(AABBMax.X - AABBMin.X, 0, 0));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, AABBMax.Z - AABBMin.Z) + new Vector3(AABBMax.X - AABBMin.X, 0, 0));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, 0) + new Vector3(AABBMax.X - AABBMin.X, 0, 0));


            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z) + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, AABBMax.Z - AABBMin.Z) + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, 0) + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));

            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, AABBMax.Y - AABBMin.Y, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, AABBMax.Z - AABBMin.Z));


            GL.End();

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Begin(PrimitiveType.Quads);
            GL.Color4(1, 0, 0, 1f);
            GL.Vertex3(AABBMin);
            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));


            GL.Vertex3(AABBMin);
            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, 0));


            GL.Vertex3(AABBMin);
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, 0));


            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, 0));
            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z) + new Vector3(AABBMax.X - AABBMin.X, 0, 0));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, AABBMax.Z - AABBMin.Z) + new Vector3(AABBMax.X - AABBMin.X, 0, 0));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, 0) + new Vector3(AABBMax.X - AABBMin.X, 0, 0));


            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z) + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, AABBMax.Z - AABBMin.Z) + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, 0) + new Vector3(0, AABBMax.Y - AABBMin.Y, 0));

            GL.Vertex3(AABBMin + new Vector3(0, 0, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(0, AABBMax.Y - AABBMin.Y, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, AABBMax.Y - AABBMin.Y, AABBMax.Z - AABBMin.Z));
            GL.Vertex3(AABBMin + new Vector3(AABBMax.X - AABBMin.X, 0, AABBMax.Z - AABBMin.Z));


            GL.End();
        }

    }
}
