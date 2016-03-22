using ColladaDotNet;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Factory
{
    [System.SerializableAttribute()]
    public class ColladaGeometry
    {
        public List<Triangle> triangles = new List<Triangle>();
        public List<GeometryLine> geometryLines = new List<GeometryLine>();
        public static ColladaGeometry LoadDAE(string DAE_NAME)
        {
            if (!File.Exists(Constants.CacheDir + DAE_NAME + ".engine_cached_model"))
            {
                //DEBUG LOGGING
                DebugTools.Log("[Model Loader]: No cached geometry found, parsing now.");
                //No Cache, load it the old way
                var Collada = ColladaDotNet.Collada.Load_File(DAE_NAME);
                var geometry = ColladaGeometry.LoadGeometries(Collada);
                //and save the cache
                geometry.Save(Constants.CacheDir + DAE_NAME + ".engine_cached_model");
                return geometry;
            }
            else
            {
                return ColladaGeometry.Load(Constants.CacheDir + DAE_NAME + ".engine_cached_model");
            }
        }
        public void Save(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            Directory.CreateDirectory(fi.Directory.FullName);
            XmlSerializer sr = new XmlSerializer(typeof(ColladaGeometry));
            GZipStream tr =new GZipStream(new FileStream(filename, FileMode.Create, FileAccess.ReadWrite), CompressionMode.Compress);
            sr.Serialize(tr, this);
            tr.Close();

        }
        public static ColladaGeometry Load(string filename)
        {
            XmlSerializer sr = new XmlSerializer(typeof(ColladaGeometry));
            GZipStream tr = new GZipStream(new FileStream(filename, FileMode.Open, FileAccess.ReadWrite),CompressionMode.Decompress);
            var geometry = (ColladaGeometry)sr.Deserialize(tr);
            tr.Close();
            return geometry;

        }

        public static ColladaGeometry LoadGeometries(ColladaDotNet.Collada Collada)
        {
            Matrix4 transform = new Matrix4(
                1, 0, 0, 0, //x_x, x_y, x_z, t
                0, 1, 0, 0, //y_x, y_y, y_z, t
                0, 0, 1, 0, //z_x, z_y, z_z, t
                0, 0, 0, 1
                );
            List<ColladaGeometry> geometries = new List<ColladaGeometry>();
            //Build the geometry index
            Dictionary<String, Collada_Geometry> geometryNodes = new Dictionary<string, Collada_Geometry>();
            //DEBUG LOGGING
            DebugTools.Log("[Model Loader]: Creating Geometry Dictionary");
            foreach (Collada_Geometry geometry in Collada.Library_Geometries.Geometry)
            {
                geometryNodes.Add("#" + geometry.ID, geometry); //Number sign for easier look ups
            }
            //Build the material ID index
            Dictionary<String, Color> effectIdNodes = new Dictionary<string, Color>();
            //DEBUG LOGGING
            DebugTools.Log("[Model Loader]: Creating Effect Dictionary");
            foreach (Collada_Effect effect in Collada.Library_Effects.Effect)
            {
                //Check that it's a valid effect
                if (!(effect.Profile_COMMON[0].Technique.Lambert == null))
                {
                    if (effect.Profile_COMMON[0].Technique.Lambert.Diffuse.Color != null)
                    {
                        float[] RGBA = effect.Profile_COMMON[0].Technique.Lambert.Diffuse.Color.Value();
                        Color color = Color.FromArgb(
                            (int)(RGBA[3] * 255),
                            (int)(RGBA[0] * 255),
                            (int)(RGBA[1] * 255),
                            (int)(RGBA[2] * 255));
                        if (RGBA.Average() == 0)
                        {
                            color = Color.Black;
                        }
                        effectIdNodes.Add("#" + effect.ID, color);
                    }
                    else
                    {
                        effectIdNodes.Add("#" + effect.ID, Color.Black);
                    }
                }
                else
                {
                    //it might be a constant
                    if (!(effect.Profile_COMMON[0].Technique.Constant == null))
                    {
                        float[] RGBA = effect.Profile_COMMON[0].Technique.Constant.Transparent.Color.Value();
                        Color color = Color.FromArgb(
                            (int)(RGBA[3] * 255),
                            (int)(RGBA[0] * 255),
                            (int)(RGBA[1] * 255),
                            (int)(RGBA[2] * 255));
                        if (RGBA.Average() == 0)
                        {
                            color = Color.Black;
                        }
                        effectIdNodes.Add("#" + effect.ID, color);
                    }
                    else
                    {
                        effectIdNodes.Add("#" + effect.ID, Color.Black);
                        //DEBUG LOGGING
                        DebugTools.Log("[Model Loader]: Effect with ID " + effect.ID + " has an invalid material, assuming black.");
                    }
                }
            }
            //Build the color ID index
            Dictionary<String, Color> colorNodes = new Dictionary<string, Color>();
            //DEBUG LOGGING
            DebugTools.Log("[Model Loader]: Creating Color Dictionary");
            foreach (Collada_Material material in Collada.Library_Materials.Material)
            {
                colorNodes.Add("#" + material.ID,
                    effectIdNodes[material.Instance_Effect.URL]); //Resolve the material effect Id
            }
            //and build individual geometry nodes
            //DEBUG LOGGING
            DebugTools.Log("[Model Loader]: Building Node Dictionary");
            Dictionary<String, Collada_Node> nodeNodes = new Dictionary<string, Collada_Node>();
            foreach (Collada_Node node in Collada.Library_Visual_Scene.Visual_Scene[0].Node)
            {
                recursiveAddNodes(node, ref nodeNodes);
            }
            if (Collada.Library_Nodes != null)
            {

                foreach (Collada_Node node in Collada.Library_Nodes.Node)
                {
                    recursiveAddNodes(node, ref nodeNodes);
                }
            }
            DebugTools.Log("[Model Loader]: Parsing Geometry, " + nodeNodes.Count + " nodes.");
            foreach (Collada_Node node in nodeNodes.Values)
            {
                ParseGeometryFromNode(node, transform, ref colorNodes, ref geometries, ref geometryNodes, ref nodeNodes);
            }
            //DEBUG LOGGING
            DebugTools.Log("[Model Loader]: Removing duplicate triangles.");
            //and output
            ColladaGeometry outputGeometry = new ColladaGeometry();
            //and finally, merge all of the geometries into one single one
            foreach (ColladaGeometry geometry in geometries)
            {
                outputGeometry.geometryLines.AddRange(geometry.geometryLines);
                outputGeometry.triangles.AddRange(geometry.triangles);
            }
            int preOptimization = outputGeometry.triangles.Count;
            int i = 0;
            if (outputGeometry.triangles.Count < 100000)
            {
                do
                {
                    //DEBUG LOGGING
                    DebugTools.Log("[Model Loader]: Parsed " + i + "/" + outputGeometry.triangles.Count);
                    List<Triangle> removeList = new List<Triangle>();
                    foreach (Triangle t in outputGeometry.triangles)
                    {
                        var x1 = outputGeometry.triangles[i].vertices[0].X +
                            outputGeometry.triangles[i].vertices[1].X +
                            outputGeometry.triangles[i].vertices[2].X;
                        var y1 = outputGeometry.triangles[i].vertices[0].Y +
                            outputGeometry.triangles[i].vertices[1].Y +
                            outputGeometry.triangles[i].vertices[2].Y;
                        var z1 = outputGeometry.triangles[i].vertices[0].Z +
                            outputGeometry.triangles[i].vertices[1].Z +
                            outputGeometry.triangles[i].vertices[2].Z;

                        var x2 = t.vertices[0].X +
                            t.vertices[1].X +
                            t.vertices[2].X;
                        var y2 = t.vertices[0].Y +
                            t.vertices[1].Y +
                            t.vertices[2].Y;
                        var z2 = t.vertices[0].Z +
                            t.vertices[1].Z +
                            t.vertices[2].Z;
                        if (x1 == x2 & y1 == y2 & z1 == z2 &
                            !outputGeometry.triangles[i].Equals(t))
                        {
                            //DEBUG LOGGING
                            DebugTools.Log("[Model Loader]: Clipping triangles - {" +
                                t.vertices[0].X.ToString("N1") + "," +
                                t.vertices[0].Y.ToString("N1") + "," +
                                t.vertices[0].Z.ToString("N1") + "}{" +

                                t.vertices[0].X.ToString("N1") + "," +
                                t.vertices[1].Y.ToString("N1") + "," +
                                t.vertices[2].Z.ToString("N1") + "}{" +

                                t.vertices[0].X.ToString("N1") + "," +
                                t.vertices[1].Y.ToString("N1") + "," +
                                t.vertices[2].Z.ToString("N1") + "}");
                            removeList.Add(t);
                            break; //Usually only one duplicate
                        }
                    }

                    foreach (Triangle t in removeList)
                    {
                        outputGeometry.triangles.Remove(t);
                    }
                    i++;
                } while (i < outputGeometry.triangles.Count);
            }
            else
            {
                //DEBUG LOGGING
                DebugTools.Log("[Model Loader]: Skipping polygon optimization, too many triangles to do so.");
            }

            //DEBUG LOGGING
            DebugTools.Log("[Model Loader]: Pre-optimization: " + preOptimization + " tri's, Post-optimization: " + outputGeometry.triangles.Count + " tri's");
            DebugTools.Log("[Model Loader]: Lines: " + outputGeometry.geometryLines.Count);
            //DEBUG LOGGING
            DebugTools.Log("[Model Loader]: Shifting geometry to center.");

            var min = new Vector3(99999999999, 999999999999, 99999999999);
            var max = new Vector3(0, 0, 0);
            foreach (Triangle tri in outputGeometry.triangles)
            {
                foreach (Vector3 vertex in tri.vertices)
                {
                    if (vertex.X < min.X) min.X = vertex.X;

                    if (vertex.Y < min.Y) min.Y = vertex.Y;

                    if (vertex.Z < min.Z) min.Z = vertex.Z;


                    if (vertex.X > max.X) max.X = vertex.X;

                    if (vertex.Y > max.Y) max.Y = vertex.Y;

                    if (vertex.Z > max.Z) max.Z = vertex.Z;

                }
            }
            Vector3 shiftVector = new Vector3((max + min) / 2);
            //DEBUG LOGGING
            DebugTools.Log("[Model Loader]: Shifting Model by " + shiftVector.ToString());
            foreach (Triangle tri in outputGeometry.triangles)
            {
                tri.vertices[0] = tri.vertices[0] - shiftVector;
                tri.vertices[1] = tri.vertices[1] - shiftVector;
                tri.vertices[2] = tri.vertices[2] - shiftVector;
            }
            foreach (GeometryLine line in outputGeometry.geometryLines)
            {
                line.vertices[0] = line.vertices[0] - shiftVector;
                line.vertices[1] = line.vertices[1] - shiftVector;
            }
            //and return
            return outputGeometry;
        }
        public static void recursiveAddNodes(Collada_Node node, ref Dictionary<string, Collada_Node> nodeNodes)
        {
            if (node.ID != null)
            {
                nodeNodes.Add("#" + node.ID, node);
            }
            else
            {
                nodeNodes.Add("#" + node.Name, node);

            }
            if (node.node != null)
            {
                foreach (Collada_Node node_sub in node.node)
                {
                    recursiveAddNodes(node_sub, ref nodeNodes);
                }
            }
        }
        public static void ParseGeometryFromNode(Collada_Node geom_node, Matrix4 transform, ref Dictionary<string, Color> colorNodes, ref List<ColladaGeometry> geometries, ref Dictionary<string, Collada_Geometry> geometryNodes, ref Dictionary<string, Collada_Node> nodeNodes)
        {
            //if there is no matrix...
            if (geom_node.Matrix == null)
            {
                transform = new Matrix4(
                    1, 0, 0, 0, //x_x, x_y, x_z, t
                    0, 1, 0, 0, //y_x, y_y, y_z, t
                    0, 0, 1, 0, //z_x, z_y, z_z, t
                    0, 0, 0, 1
                    );
            }
            else
            {
                //get the matrix
                float[] matrix = geom_node.Matrix[0].Value();
                //map to the transform
                //      [axis]_x                  [axis]_y                   [axis]_z                [axis]_transform
                transform.M11 = matrix[0]; transform.M12 = matrix[1]; transform.M13 = matrix[2]; transform.M14 = matrix[3]; //X axis
                transform.M21 = matrix[4]; transform.M22 = matrix[5]; transform.M23 = matrix[6]; transform.M24 = matrix[7]; //Y axis
                transform.M31 = matrix[8]; transform.M32 = matrix[9]; transform.M33 = matrix[10]; transform.M34 = matrix[11]; //Z axis
                transform.M41 = 0; transform.M42 = matrix[9]; transform.M43 = matrix[10]; transform.M44 = matrix[11]; //W axis
            }
            //and now process the geometry
            if (geom_node.Instance_Geometry != null)
            {
                foreach (Collada_Instance_Geometry geometry in geom_node.Instance_Geometry)
                {
                    //build a dictionary of materials
                    Dictionary<string, Color> Materials = new Dictionary<string, Color>();
                    foreach (Collada_Instance_Material_Geometry materialName in
                        geometry.Bind_Material[0].Technique_Common.Instance_Material)
                    {
                        Materials.Add(materialName.Symbol, colorNodes[materialName.Target]);
                    }
                    //and get the actual geometry
                    geometries.Add(parseGeometry(transform, geometryNodes[geometry.URL], Materials));
                }

            }
            //and now process the sub nodes
            if (geom_node.Instance_Node != null)
            {
                foreach (Collada_Instance_Node node in geom_node.Instance_Node)
                {
                    nodeNodes[node.URL].Matrix = geom_node.Matrix;

                    ParseGeometryFromNode(nodeNodes[node.URL], transform, ref colorNodes, ref geometries, ref geometryNodes, ref nodeNodes);

                }

            }
            else
            {
                //DEBUG LOGGING
                DebugTools.Log("[Model Loader]: Geometry missing in node with ID " + geom_node.ID);
            }
        }

        public static ColladaGeometry parseGeometry(Matrix4 transform, Collada_Geometry geometry, Dictionary<string, Color> colors)
        {
            //Parse all of the geometry
            ColladaGeometry geom = new ColladaGeometry();
            //get the raw geometry
            float[] Vertices = geometry.Mesh.Source[0].Float_Array.Value();
            float[] Normals = { };
            if (geometry.Mesh.Source.Length > 1)
            {
                Normals = geometry.Mesh.Source[1].Float_Array.Value(); //the normals
            }
            //build the vertex and normal lookup table
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            //and parse
            for (int i = 0; i < Vertices.Length; i += 3)
            {
                Vector3 vertex = new Vector3(Vertices[i], Vertices[i + 1], Vertices[i + 2]).MultiplyByMatrix4(transform);
                if (geometry.Mesh.Source.Length > 1)
                {
                    Vector3 normal = new Vector3(Normals[i], Normals[i + 1], Normals[i + 2]); //.MultiplyByMatrix3x4(transform);
                    normals.Add(normal);
                }
                //add to list
                vertices.Add(vertex);
            }
            //and now construct triangle geometry from that
            if (!(geometry.Mesh.Triangles == null))
            {
                foreach (Collada_Triangles t in geometry.Mesh.Triangles)
                {
                    int[] geometryIds = t.P.Value();
                    //and parse the list through the lookup table
                    for (int i = 0; i < geometryIds.Length; i += 3)
                    {
                        Triangle tri = new Triangle();
                        tri.vertices[0] = vertices[geometryIds[i]];
                        tri.normals[0] = normals[geometryIds[i]];

                        tri.vertices[1] = vertices[geometryIds[i + 1]];
                        tri.normals[1] = normals[geometryIds[i + 1]];

                        tri.vertices[2] = vertices[geometryIds[i + 2]];
                        tri.normals[2] = normals[geometryIds[i + 2]];

                        //color
                        tri.color = colors[t.Material];

                        //and add it to the triangles list
                        geom.triangles.Add(tri);
                    }
                }
            }
            else
            {
                //DEBUG LOGGING
                //PRINT THE GEOMETRY ID IF NO TRIANGLES
                DebugTools.Log("[Model Loader]: No triangles in geometry with ID " + geometry.ID);
            }

            if (!(geometry.Mesh.Lines == null))
            {
                //and now the lines...
                int[] lineIds = geometry.Mesh.Lines[0].P.Value();
                //and parse the list through the lookup table
                for (int i = 0; i < lineIds.Length; i += 2)
                {
                    GeometryLine line = new GeometryLine();
                    //build the vertex list
                    line.vertices[0] = vertices[lineIds[i]];

                    line.vertices[1] = vertices[lineIds[i + 1]];

                    //and add it to the lines list
                    geom.geometryLines.Add(line);
                }
            }
            else
            {
                //DEBUG LOGGING
                //PRINT THE GEOMETRY ID IF NO LINES
                DebugTools.Log("[Model Loader]: No lines in geometry with ID " + geometry.ID);
            }

            return geom;
        }


    }

    public class Triangle
    {
        public bool duplicate = false;
        public Color4 color = Color4.Black;
        public Vector3[] vertices = new Vector3[3];
        public Vector3[] normals = new Vector3[3];
        public Vector3[] texture = new Vector3[3];
        public bool hasTexture = false;
    }
    public class GeometryLine
    {
        public bool duplicate = false;
        public Vector3[] vertices = new Vector3[2];

    }
}
