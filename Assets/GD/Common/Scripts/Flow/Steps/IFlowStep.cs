using System.Collections;

namespace GD.Flow
{
    /// <summary>
    /// One stage of a flow. Run must finish, or the sequence it belongs to never continues.
    /// </summary>
    public interface IFlowStep
    {
        IEnumerator Run();
    }
}
