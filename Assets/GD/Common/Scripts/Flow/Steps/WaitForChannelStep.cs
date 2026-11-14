using System.Collections;
using GD.Types;
using UnityEngine;

namespace GD.Flow
{
    /// <summary>
    /// Waits until a channel is raised. Only raises after this step starts are counted.
    /// </summary>
    public class WaitForChannelStep : IFlowStep
    {
        private readonly VoidEventChannel channel;

        public WaitForChannelStep(VoidEventChannel channel)
        {
            this.channel = channel;
        }

        public override string ToString()
        {
            return $"Wait for {(channel != null ? channel.name : "(no channel)")}";
        }

        public IEnumerator Run()
        {
            bool raised = false;

            void HandleRaised()
            {
                raised = true;
            }

            channel.Raised += HandleRaised;
            yield return new WaitUntil(() => raised);
            channel.Raised -= HandleRaised;
        }
    }
}
