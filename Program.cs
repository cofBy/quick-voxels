using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelRenderer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (Main game = new Main(800, 600, "LearnOpenTK"))
            {
                game.Run();
            }
        }
    }
}
