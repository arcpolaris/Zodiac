using System.Diagnostics.CodeAnalysis;

namespace Zodiac.Util
{
    public class StateMachine<TContext>
    {
        private readonly TContext context;

        public State<TContext> State { get; private set; }

        public StateMachine(TContext context, State<TContext> initial)
        {
            this.context = context;
            State = initial;
        }

        public void Update()
        {
            if (!State.Update(context, out var next)) return;
            State.OnExit(context);
            State = next;
            State.OnEnter(context);
        }
    }

    public abstract class State<TContext>
    {
        public abstract bool Update(TContext context, [MaybeNull, NotNullWhen(true)] out State<TContext> next);
        public virtual void OnEnter(TContext context) { }
        public virtual void OnExit(TContext context) { }
    }
}