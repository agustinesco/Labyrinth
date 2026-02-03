using NUnit.Framework;
using Labyrinth.Maze;

namespace Labyrinth.Tests
{
    public class MazeGeneratorTests
    {
        [Test]
        public void Generate_CreatesGridWithCorrectSize()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();
            Assert.AreEqual(25, grid.Width);
            Assert.AreEqual(25, grid.Height);
        }

        [Test]
        public void Generate_StartCellIsFloor()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();
            var start = grid.GetCell(1, 1);
            Assert.IsFalse(start.IsWall);
            Assert.IsTrue(start.IsStart);
        }

        [Test]
        public void Generate_HasExitCell()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();

            bool hasExit = false;
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    if (grid.GetCell(x, y).IsExit)
                    {
                        hasExit = true;
                        break;
                    }
                }
            }
            Assert.IsTrue(hasExit);
        }

        [Test]
        public void Generate_HasFloorCells()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();

            int floorCount = 0;
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    if (!grid.GetCell(x, y).IsWall)
                        floorCount++;
                }
            }
            Assert.Greater(floorCount, 50);
        }

        [Test]
        public void Generate_OuterEdgesAreWalls()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();

            for (int x = 0; x < grid.Width; x++)
            {
                Assert.IsTrue(grid.GetCell(x, 0).IsWall);
                Assert.IsTrue(grid.GetCell(x, grid.Height - 1).IsWall);
            }
            for (int y = 0; y < grid.Height; y++)
            {
                Assert.IsTrue(grid.GetCell(0, y).IsWall);
                Assert.IsTrue(grid.GetCell(grid.Width - 1, y).IsWall);
            }
        }
    }
}
