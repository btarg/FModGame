public class StateMachine<T> where T : IState
{
    private T currentState;

    public void Tick()
    {
        currentState?.Tick();
    }

    public void SetState(T state)
    {
        currentState?.OnExit();
        currentState = state;
        currentState.OnEnter();
    }
}

public interface IState
{
    void OnEnter();
    void Tick();
    void OnExit();
}