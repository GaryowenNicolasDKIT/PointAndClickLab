namespace GD.States
{
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}
