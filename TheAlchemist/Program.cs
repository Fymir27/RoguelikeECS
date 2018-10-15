using System;

namespace TheAlchemist
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                using (var game = new Game())
                    game.Run();
            }
            catch(NullReferenceException e)
            {
                Console.WriteLine("[Error] Whoops, something appears to be missing!" + "\n" + e.StackTrace);
            }
        }
    }
}
