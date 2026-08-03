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
            using (Main game = new Main(1920, 1080, "QuickRenderer"))
            {
                game.Run();
            }
        }
    }
}
