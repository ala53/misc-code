using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Factory.AI
{
    /// <summary>
    /// Provides a class to manage the implementation of AI waypoints.
    /// </summary>
    class WaypointManager
    {
        public Dictionary<int, Waypoint> Waypoints = new Dictionary<int, Waypoint>();
        public Waypoint CurrentWaypoint;
        int CurrentID = 7;
        /// <summary>
        /// Adds a waypoint linked to the currently selected one.
        /// If no waypoint is selected, it adds a new waypoint.
        /// </summary>
        /// <param name="Location"></param>
        public int AddWaypoint(Vector3 Location)
        {
            var wp = new Waypoint(Location, CurrentID);
            Waypoints.Add(CurrentID, wp);
            CurrentID++;
            if (CurrentWaypoint != null)
            {
                //Because the links are two way
                wp.AttachedWaypoints.Add(CurrentWaypoint.ID);
                CurrentWaypoint.AttachedWaypoints.Add(wp.ID);
            }
            else
            {
                SwitchCurrentWaypoint(wp.ID);
            }

            return wp.ID;
        }
        /// <summary>
        /// Adds a waypoint linked to the currently selected one
        /// and then sets it as selected.
        /// </summary>
        /// <param name="Location"></param>
        public void AddAndSwitchWaypoint(Vector3 Location)
        {
            SwitchCurrentWaypoint(AddWaypoint(Location));
        }
        /// <summary>
        /// Allows you to switch the current waypoint that 
        /// operations are performed on.
        /// </summary>
        /// <param name="WaypointID"></param>
        public void SwitchCurrentWaypoint(int WaypointID)
        {
            CurrentWaypoint = Waypoints[WaypointID];
        }

        public void SetAISpawner(int WaypointID, bool Spawner)
        {
            Waypoints[WaypointID].IsAISpawner = Spawner;
        }
        /// <summary>
        /// Allows you to link an existing waypoint to the current
        /// waypoint, linking the path.
        /// </summary>
        /// <param name="WaypointID"></param>
        public void LinkExistingWaypoint(int WaypointID)
        {
            //Remember, all links are two way
            CurrentWaypoint.AttachedWaypoints.Add(WaypointID);
            Waypoints[WaypointID].AttachedWaypoints.Add(CurrentWaypoint.ID);
        }
        /// <summary>
        /// Removes a link to the specified waypoint from the 
        /// current selected one.
        /// </summary>
        /// <param name="WaypointID"></param>
        public void RemoveWaypointLink(int WaypointID)
        {
            //Remember, all links are two way
            CurrentWaypoint.AttachedWaypoints.Remove(WaypointID);
            Waypoints[WaypointID].AttachedWaypoints.Remove(CurrentWaypoint.ID);

        }

        /// <summary>
        /// Removes the waypoint with the specified ID
        /// entirely and pops the stack, setting related
        /// waypoints as Top level.
        /// This is a very slow O (n^n) search due to
        /// having to do a inner-inner loop search for 
        /// links. Please avoid using on render thread.
        /// </summary>
        /// <param name="WaypointID"></param>
        public void DeleteWaypoint(int WaypointID)
        {
            //build a list of waypoints that are affected
            HashSet<Waypoint> waypoints = new HashSet<Waypoint>();
            foreach (int wp in Waypoints[WaypointID].AttachedWaypoints)
            {
                waypoints.Add(GetWaypoint(wp));
            }
            //Delete the waypoint
            Waypoints.Remove(WaypointID);
            foreach (Waypoint p in waypoints)
            {
                //And because all links are two way
                p.AttachedWaypoints.Remove(WaypointID);
            }
        }
        /// <summary>
        /// Gets an array of Waypoints that are linked to
        /// the currently selected waypoint.
        /// </summary>
        /// <param name="WaypointID"></param>
        /// <returns></returns>
        public Waypoint[] GetLinkedWaypoints(int WaypointID)
        {
            List<Waypoint> waypoints = new List<Waypoint>();
            foreach (int wp in Waypoints[WaypointID].AttachedWaypoints)
            {
                waypoints.Add(GetWaypoint(wp));
            }

            return waypoints.ToArray();
        }
        /// <summary>
        /// Gets a waypoint with the specified ID.
        /// </summary>
        /// <param name="WaypointID"></param>
        /// <returns></returns>
        public Waypoint GetWaypoint(int WaypointID)
        {
            return Waypoints[WaypointID];

        }
    }

    public class Waypoint
    {
        public Vector3 Location;
        public HashSet<int> AttachedWaypoints;
        public bool IsAISpawner;
        public int ID;
        public Waypoint(Vector3 location, int id)
        {
            location.Z = 0;
            ID = id;
            Location = location;
            IsAISpawner = false;
            AttachedWaypoints = new HashSet<int>();
        }
    }
}
