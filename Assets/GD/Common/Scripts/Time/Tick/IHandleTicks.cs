namespace GD.Tick
{
    /// <summary>
    /// Any class that implements this interface can register with TimeTickSystem
    /// to run at a fixed rate (every 1, 2, 4 or 8 ticks) instead of every frame.
    /// </summary>
    /// <remarks>
    /// A MonoBehaviour usually extends TickBehaviour, which registers and
    /// unregisters for it. Implement this interface directly when the class
    /// already has a base class, or is not a MonoBehaviour.
    /// </remarks>
    /// <see cref="TimeTickSystem"/>
    /// <see cref="TickBehaviour"/>
    public interface IHandleTicks
    {
        /// <summary>
        /// Called once per tick at the registered rate. Keep it short, because
        /// every handler due on a tick runs in the same frame.
        /// </summary>
        void HandleTick();
    }
}
