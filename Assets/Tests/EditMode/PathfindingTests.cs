using NUnit.Framework;
using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Tests
{
    public class PathfindingTests
    {
        private MazeGrid CreateSimpleGrid()
        {
            var grid = new MazeGrid(5, 5);
            // Create a simple corridor: floor at (1,1), (2,1), (3,1)
            grid.GetCell(1, 1).IsWall = false;
            grid.GetCell(2, 1).IsWall = false;
            grid.GetCell(3, 1).IsWall = false;
            return grid;
        }

        [Test]
        public void FindPath_ReturnsPath_WhenPathExists()
        {
            var grid = CreateSimpleGrid();
            var pathfinder = new Pathfinding(grid);

            var path = pathfinder.FindPath(new Vector2Int(1, 1), new Vector2Int(3, 1));

            Assert.IsNotNull(path);
            Assert.Greater(path.Count, 0);
        }

        [Test]
        public void FindPath_ReturnsNull_WhenNoPathExists()
        {
            var grid = new MazeGrid(5, 5);
            grid.GetCell(1, 1).IsWall = false;
            grid.GetCell(3, 3).IsWall = false;
            // No connection between (1,1) and (3,3)

            var pathfinder = new Pathfinding(grid);
            var path = pathfinder.FindPath(new Vector2Int(1, 1), new Vector2Int(3, 3));

            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_StartsAtStart_EndsAtGoal()
        {
            var grid = CreateSimpleGrid();
            var pathfinder = new Pathfinding(grid);

            var start = new Vector2Int(1, 1);
            var goal = new Vector2Int(3, 1);
            var path = pathfinder.FindPath(start, goal);

            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
        }

        [Test]
        public void FindPath_DoesNotGoThroughWalls()
        {
            var grid = CreateSimpleGrid();
            var pathfinder = new Pathfinding(grid);

            var path = pathfinder.FindPath(new Vector2Int(1, 1), new Vector2Int(3, 1));

            foreach (var point in path)
            {
                Assert.IsFalse(grid.GetCell(point.x, point.y).IsWall);
            }
        }
    }
}
