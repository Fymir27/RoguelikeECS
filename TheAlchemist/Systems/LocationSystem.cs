using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Systems
{
    using Components;
    
    /// <summary>
    /// static helper class with functions concerning Position/Pathfinding/Distance
    /// </summary>
    static class LocationSystem
    {
        /// <summary>
        /// tries to find an entity
        /// </summary>
        /// <param name="entity">ID of entity</param>
        /// <returns> Position of entity on current floor or null if not found </returns>
        public static Position GetPosition(int entity)
        {
            var transform = EntityManager.GetComponent<TransformComponent>(entity);

            if (transform == null)
            {
                Log.Error("Could not get Position, no TransformComponent attached! " + DescriptionSystem.GetNameWithID(entity));
                return Position.Zero;
            }

            return transform.Position;
        }

        /// <summary>
        /// calculates WALKING Distance to entity
        /// </summary>
        /// <param name="entity">ID of entity</param>
        /// <param name="from">current Position</param>
        /// <returns>distance from "from" to Position of entity</returns>
        public static int GetDistance(int entity, Position from)
        {
            var findable = EntityManager.GetComponent<FindableComponent>(entity);

            if (findable == null)
            {
                // TODO: initialize new FindableComponent instead?
                Log.Warning(String.Format("Distance to {0} could not be calculated!", DescriptionSystem.GetNameWithID(entity)));
                return int.MaxValue;
            }

            return findable.DistanceMap[from.X, from.Y];
        }

        /// <summary>
        /// calculates the next step on the fastest path towards entity
        /// </summary>
        /// <param name="entity">ID of entity</param>
        /// <param name="from">current Position</param>
        /// <returns> Position of next step </returns>
        public static Position StepTowards(int entity, Position from)
        {
            var findable = EntityManager.GetComponent<FindableComponent>(entity);

            if(findable == null)
            {
                // TODO: initialize new FindableComponent instead?
                Log.Warning(String.Format("Path to {0} could not be found!", DescriptionSystem.GetNameWithID(entity)));
                return from;
            }

            Position result = from;

            // it is assumed, that findable is up to date
            int minDistance = int.MaxValue;
            foreach (var pos in from.GetNeighbours())
            {
                int distance = findable.DistanceMap[pos.X, pos.Y];
                if(distance < minDistance)
                {
                    result = pos;
                }
                // if two tiles are the same distance, randomly decide between them
                // TODO: make even distribution
                else if(distance == minDistance && Game.Random.Next(0, 2) == 0)
                {
                    result = pos;
                }
            }

            return result;
        }

        /// <summary>
        /// Updates the distance map of a findable entity
        /// </summary>
        /// <param name="entity">ID of entity</param>
        public static void UpdateDistanceMap(int entity)
        {
            var findable = EntityManager.GetComponent<FindableComponent>(entity);

            if (findable == null)
            {
                // TODO: initialize new FindableComponent instead?
                Log.Warning(String.Format("Could not update DistanceMap of {0}, because no FindableComponent was found!", DescriptionSystem.GetNameWithID(entity)));
                return;
            }

            var curPos = GetPosition(entity);

            if(curPos == findable.LastKnownPosition)
            {
                return;
            }

            if(findable.DistanceMap == null)
            {
                findable.DistanceMap = new int[Util.CurrentFloor.Width, Util.CurrentFloor.Height];
            }

            // fill array

            for (int i = 0; i < findable.DistanceMap.Length; i++)
            {
                findable.DistanceMap.GetValue(i);
            }

            Queue<Position> todo = new Queue<Position>();
            todo.Enqueue(curPos);
            int curDistance = 0;

            while(true)
            {
                foreach (var pos in todo)
                {
                    findable.DistanceMap[pos.X, pos.Y] = curDistance;
                }
            }

            findable.LastKnownPosition = curPos;
        }
    }
}
