using NUnit.Framework;
using Labyrinth.Maze;

namespace Labyrinth.Tests
{
    public class MazeCellTests
    {
        [Test]
        public void NewCell_IsWall_ByDefault()
        {
            var cell = new MazeCell(0, 0);
            Assert.IsTrue(cell.IsWall);
        }

        [Test]
        public void Cell_StoresCoordinates()
        {
            var cell = new MazeCell(5, 10);
            Assert.AreEqual(5, cell.X);
            Assert.AreEqual(10, cell.Y);
        }

        [Test]
        public void Cell_CanBeSetToFloor()
        {
            var cell = new MazeCell(0, 0);
            cell.IsWall = false;
            Assert.IsFalse(cell.IsWall);
        }
    }
}