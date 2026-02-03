using NUnit.Framework;
using Labyrinth.Maze;

namespace Labyrinth.Tests
{
    public class MazeGridTests
    {
        [Test]
        public void Grid_HasCorrectDimensions()
        {
            var grid = new MazeGrid(25, 25);
            Assert.AreEqual(25, grid.Width);
            Assert.AreEqual(25, grid.Height);
        }

        [Test]
        public void Grid_AllCellsAreWalls_Initially()
        {
            var grid = new MazeGrid(5, 5);
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    Assert.IsTrue(grid.GetCell(x, y).IsWall);
                }
            }
        }

        [Test]
        public void Grid_GetCell_ReturnsCorrectCell()
        {
            var grid = new MazeGrid(10, 10);
            var cell = grid.GetCell(3, 7);
            Assert.AreEqual(3, cell.X);
            Assert.AreEqual(7, cell.Y);
        }

        [Test]
        public void Grid_IsInBounds_ReturnsTrueForValidCoords()
        {
            var grid = new MazeGrid(10, 10);
            Assert.IsTrue(grid.IsInBounds(0, 0));
            Assert.IsTrue(grid.IsInBounds(9, 9));
            Assert.IsTrue(grid.IsInBounds(5, 5));
        }

        [Test]
        public void Grid_IsInBounds_ReturnsFalseForInvalidCoords()
        {
            var grid = new MazeGrid(10, 10);
            Assert.IsFalse(grid.IsInBounds(-1, 0));
            Assert.IsFalse(grid.IsInBounds(0, -1));
            Assert.IsFalse(grid.IsInBounds(10, 0));
            Assert.IsFalse(grid.IsInBounds(0, 10));
        }
    }
}
