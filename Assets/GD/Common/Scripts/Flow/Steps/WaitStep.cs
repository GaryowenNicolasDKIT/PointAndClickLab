using System.Collections;
using UnityEngine;

namespace GD.Flow
{
    /// <summary>
    /// Waits for a number of seconds, in scaled time or in real time.
    /// </summary>
    public class WaitStep : IFlowStep
    {
        private readonly float seconds;
        private readonly bool realtime;

        public WaitStep(float seconds, bool realtime)
        {
            this.seconds = seconds;
            this.realtime = realtime;
        }

        public override string ToString()
        {
            return realtime ? $"Wait {seconds} s, real time" : $"Wait {seconds} s";
        }

        public IEnumerator Run()
        {
            if (realtime)
                yield return new WaitForSecondsRealtime(seconds);
            else
                yield return new WaitForSeconds(seconds);
        }
    }
}
