namespace Labyrinth.Maze
{
    public class MazeGrid
    {
        public int Width { get; }
        public int Height { get; }

        private readonly MazeCell[,] _cells;

        public MazeGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new MazeCell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y] = new MazeCell(x, y);
                }
            }
        }

        public MazeCell GetCell(int x, int y)
        {
            return _cells[x, y];
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }
}
