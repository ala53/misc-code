using BulletSharp;
using BulletSharp.SoftBody;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BulletSharp.MultiThreaded;
namespace Factory.Physics
{
    class BulletPhysics
    {
        public DiscreteDynamicsWorld physics_world { get; set; }
        CollisionDispatcher dispatcher;
        DbvtBroadphase broadphase;
        AlignedCollisionShapeArray collisionShapes = new AlignedCollisionShapeArray();
        CollisionConfiguration collisionConf;

        public BulletPhysics()
        {
            collisionConf = new DefaultCollisionConfiguration();
            dispatcher = new CollisionDispatcher(collisionConf);
            broadphase = new DbvtBroadphase();

            physics_world = new DiscreteDynamicsWorld(dispatcher, broadphase, null, collisionConf);
            physics_world.Gravity = new Vector3(0, 0, -384);
        }
        Task StepTask;
        float queuedTime = 0;
        public void Step(float timeToStep)
        {
            if (StepTask == null)
            {
                StepTask = Task.Factory.StartNew(() => physics_world.StepSimulation(timeToStep));
                return;
            }
            if (StepTask.IsCompleted)
            {
                StepTask = Task.Factory.StartNew(() => StepInternal(timeToStep));
            }
            else
            {
                queuedTime += timeToStep;
            }
        }
        protected void StepInternal(float timeToStep)
        {
            try
            {
                physics_world.StepSimulation(timeToStep + queuedTime);
                queuedTime = 0;
            }
            catch (Exception)
            {
                queuedTime += timeToStep;
            }
        }
        public void StepSync(float timeToStep)
        {
            physics_world.StepSimulation(timeToStep);
        }

        public RigidBody AddStaticGeometry(ColladaGeometry geometry, Matrix4 transform, object UserData)
        {
            TriangleMesh mesh = new TriangleMesh();
            foreach (Triangle tri in geometry.triangles)
            {
                mesh.AddTriangle(tri.vertices[0], tri.vertices[1], tri.vertices[2]
                    );
            }
            CollisionShape shape = new BvhTriangleMeshShape(mesh, true);
            shape.UserObject = UserData;

            collisionShapes.Add(shape);

            RigidBody body = CreateRigidBody(0, transform, shape); //Zero mass for static body

            return body;
        }
        public RigidBody AddDynamicGeometry(ColladaGeometry geometry, Matrix4 transform, object UserData)
        {
            var min = new Vector3(99999999999, 999999999999, 99999999999);
            var max = new Vector3(0, 0, 0);
            foreach (Triangle tri in geometry.triangles)
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
            CollisionShape shape = new BoxShape((max.X - min.X) / 2, (max.Y - min.Y) / 2, (max.Z - min.Z) / 2);



            shape.UserObject = UserData;

            collisionShapes.Add(shape);

            RigidBody body = CreateRigidBody(geometry.triangles.Count, transform, shape);

            return body;
        }
        public RigidBody AddDynamicGeometryAccurateConvel(ColladaGeometry geometry, Matrix4 transform)
        {
            TriangleMesh mesh = new TriangleMesh();
            foreach (Triangle tri in geometry.triangles)
            {
                mesh.AddTriangle(
                    tri.vertices[0],
                    tri.vertices[1],
                    tri.vertices[2]
                    );
            }
            CollisionShape shape = new ConvexTriangleMeshShape(mesh);



            shape.UserObject = geometry;

            collisionShapes.Add(shape);

            RigidBody body = CreateRigidBody(geometry.triangles.Count, transform, shape);

            return body;
        }

        public RigidBody CreateRigidBody(float mass, Matrix4 startTransform, CollisionShape shape)
        {
            bool isDynamic = (mass != 0.0f);

            Vector3 localInertia = Vector3.Zero;
            if (isDynamic)
                shape.CalculateLocalInertia(mass, out localInertia);

            DefaultMotionState myMotionState = new DefaultMotionState(startTransform);

            RigidBodyConstructionInfo rbInfo = new RigidBodyConstructionInfo(mass, myMotionState, shape, localInertia);
            RigidBody body = new RigidBody(rbInfo);

            physics_world.AddRigidBody(body);

            return body;
        }

        public void RemoveRigidBody(RigidBody body)
        {
            physics_world.RemoveRigidBody(body);
            collisionShapes.Remove(body.CollisionShape);
        }
        public void Dispose()
        {
            ExitPhysics();
            GC.SuppressFinalize(this);
        }

        public void ExitPhysics()
        {
            //remove/dispose constraints
            int i;
            for (i = physics_world.NumConstraints - 1; i >= 0; i--)
            {
                TypedConstraint constraint = physics_world.GetConstraint(i);
                physics_world.RemoveConstraint(constraint);
                constraint.Dispose();
            }

            //remove the rigidbodies from the dynamics world and delete them
            for (i = physics_world.NumCollisionObjects - 1; i >= 0; i--)
            {
                CollisionObject obj = physics_world.CollisionObjectArray[i];
                RigidBody body = obj as RigidBody;
                if (body != null && body.MotionState != null)
                {
                    body.MotionState.Dispose();
                }
                physics_world.RemoveCollisionObject(obj);
                obj.Dispose();
            }

            //delete collision shapes
            foreach (CollisionShape shape in collisionShapes)
                shape.Dispose();
            collisionShapes.Clear();

            physics_world.Dispose();
            broadphase.Dispose();
            if (dispatcher != null)
            {
                dispatcher.Dispose();
            }
            collisionConf.Dispose();
        }

    }
}
