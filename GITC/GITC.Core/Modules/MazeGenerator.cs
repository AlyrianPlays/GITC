using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GITC.Core
{
    // A perfect maze (every cell reachable, exactly one path between any two cells) built with a seeded recursive backtracker.
    // The start is on the left edge and the exit on the right edge, so the solution always crosses the maze.
    internal class MazeGenerator
    {
        // Passage bits per cell: a set bit means that side is open
        public const int NORTH = 1;
        public const int EAST = 2;
        public const int SOUTH = 4;
        public const int WEST = 8;
        public const int MINSIZE = 5;
        public const int MAXSIZE = 25;

        public int width { get; set; }
        public int height { get; set; }
        public int[,] cells { get; set; }
        public Point start { get; set; }
        public Point exit { get; set; }
        // Start to exit, in order; the only route through the maze
        public List<Point> solution { get; set; }
        public HashSet<Point> solutionSet { get; set; }

        public MazeGenerator()
        {
            solution = new List<Point>();
            solutionSet = new HashSet<Point>();
        }

        public MazeGenerator(int width, int height, int seed) : this()
        {
            Generate(width, height, seed);
        }

        // Side length for a difficulty: 5x5 at difficulty 1, +2 per level, capped at 25x25
        public static int SizeFor(int difficulty)
        {
            return Math.Clamp(MINSIZE + 2 * (Math.Max(1, difficulty) - 1), MINSIZE, MAXSIZE);
        }

        public static int OffsetX(int bit)
        {
            return bit == EAST ? 1 : bit == WEST ? -1 : 0;
        }

        public static int OffsetY(int bit)
        {
            return bit == SOUTH ? 1 : bit == NORTH ? -1 : 0;
        }

        public static int Opposite(int bit)
        {
            return bit == NORTH ? SOUTH : bit == SOUTH ? NORTH : bit == EAST ? WEST : EAST;
        }

        public bool InBounds(Point p)
        {
            return p.X >= 0 && p.Y >= 0 && p.X < width && p.Y < height;
        }

        public bool IsOpen(Point p, int bit)
        {
            return InBounds(p) && (cells[p.X, p.Y] & bit) != 0;
        }

        public void Generate(int width, int height, int seed)
        {
            this.width = Math.Clamp(width, MINSIZE, MAXSIZE);
            this.height = Math.Clamp(height, MINSIZE, MAXSIZE);
            cells = new int[this.width, this.height];
            Random rng = seed == 0 ? new Random() : new Random(seed);
            int[] bits = { NORTH, EAST, SOUTH, WEST };

            // Recursive backtracker, iterative so a 25x25 maze cannot overflow the stack
            bool[,] visited = new bool[this.width, this.height];
            Stack<Point> stack = new Stack<Point>();
            start = new Point(0, rng.Next(this.height));
            exit = new Point(this.width - 1, rng.Next(this.height));
            visited[start.X, start.Y] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                Point at = stack.Peek();
                List<int> options = new List<int>();
                foreach (int bit in bits)
                {
                    Point next = new Point(at.X + OffsetX(bit), at.Y + OffsetY(bit));
                    if (InBounds(next) && !visited[next.X, next.Y])
                        options.Add(bit);
                }
                if (options.Count == 0)
                {
                    stack.Pop();
                    continue;
                }
                int chosen = options[rng.Next(options.Count)];
                Point to = new Point(at.X + OffsetX(chosen), at.Y + OffsetY(chosen));
                cells[at.X, at.Y] |= chosen;
                cells[to.X, to.Y] |= Opposite(chosen);
                visited[to.X, to.Y] = true;
                stack.Push(to);
            }

            FindSolution();
        }

        // Breadth-first search from the start; in a perfect maze the route found is the only one
        private void FindSolution()
        {
            Dictionary<Point, Point> cameFrom = new Dictionary<Point, Point>();
            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(start);
            cameFrom[start] = start;
            while (queue.Count > 0 && !cameFrom.ContainsKey(exit))
            {
                Point at = queue.Dequeue();
                foreach (int bit in new[] { NORTH, EAST, SOUTH, WEST })
                {
                    if (!IsOpen(at, bit))
                        continue;
                    Point next = new Point(at.X + OffsetX(bit), at.Y + OffsetY(bit));
                    if (cameFrom.ContainsKey(next))
                        continue;
                    cameFrom[next] = at;
                    queue.Enqueue(next);
                }
            }

            solution = new List<Point>();
            for (Point p = exit; ; p = cameFrom[p])
            {
                solution.Add(p);
                if (p == start)
                    break;
            }
            solution.Reverse();
            solutionSet = new HashSet<Point>(solution);
        }

        // Number of open passages between cells; a perfect maze has exactly width*height - 1
        public int CountPassages()
        {
            int total = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if ((cells[x, y] & EAST) != 0) total++;
                    if ((cells[x, y] & SOUTH) != 0) total++;
                }
            return total;
        }

        // S = start, E = exit, . = solution path
        public string ToAscii()
        {
            StringBuilder sb = new StringBuilder();
            for (int x = 0; x < width; x++)
                sb.Append("+--");
            sb.AppendLine("+");
            for (int y = 0; y < height; y++)
            {
                StringBuilder row = new StringBuilder();
                StringBuilder floor = new StringBuilder();
                row.Append(new Point(0, y) == start ? ' ' : '|');
                floor.Append('+');
                for (int x = 0; x < width; x++)
                {
                    Point p = new Point(x, y);
                    char mark = p == start ? 'S' : p == exit ? 'E' : solutionSet.Contains(p) ? '.' : ' ';
                    row.Append(' ').Append(mark);
                    row.Append((cells[x, y] & EAST) != 0 || p == exit ? ' ' : '|');
                    floor.Append((cells[x, y] & SOUTH) != 0 ? "  " : "--").Append('+');
                }
                sb.AppendLine(row.ToString());
                sb.AppendLine(floor.ToString());
            }
            return sb.ToString();
        }
    }
}
