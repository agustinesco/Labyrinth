namespace Labyrinth.Maze
{
    public class MazeCell
    {
        public int X { get; }
        public int Y { get; }
        public bool IsWall { get; set; } = true;
        public bool IsVisited { get; set; }
        public bool IsStart { get; set; }
        public bool IsExit { get; set; }
        public bool IsKeyRoom { get; set; }

        public MazeCell(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}