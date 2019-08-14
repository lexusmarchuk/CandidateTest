using System;

namespace CandidateTest.Threads
{
    public static class SafeExecute
    {
        public static bool Sync(Action action, bool logException = true)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                if (logException)
                {
                    Console.WriteLine(ex.Message);
                }

                return false;
            }
        }
    }
}
