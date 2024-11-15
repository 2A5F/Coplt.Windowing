using System.Collections.Concurrent;

namespace Coplt.Windowing;

#region EventId

public readonly record struct EventId
{
    public ulong Id { get; }
    private readonly EventBus.AOff off;
    internal EventId(ulong Id, EventBus.AOff off)
    {
        this.Id = Id;
        this.off = off;
    }

    public void Off() => off.Off(Id);
}

#endregion

#region EventBus

public class EventBus
{
    #region Impl

    private static ulong s_event_id_inc;

    internal static ulong AllocId() => Interlocked.Increment(ref s_event_id_inc);

    internal readonly ConcurrentDictionary<Type, Bus> m_events = new();

    internal abstract class Bus;

    internal abstract class AOff
    {
        internal abstract void Off(ulong id);
    }

    internal class Bus<E> : Bus
    {
        internal readonly ConcurrentDictionary<Type, Class> m_classes = new();

        internal abstract class Class : AOff
        {
            internal abstract void Emit(E ev);
        }

        internal class Class<A> : Class
        {
            internal readonly ConcurrentDictionary<ulong, (Action<E, A> handler, A arg)> m_handlers = new();
            internal ConcurrentDictionary<ulong, (Action<E, A> handler, A arg)> m_once_handlers = new();
            internal readonly ConcurrentQueue<ConcurrentDictionary<ulong, (Action<E, A> handler, A arg)>>
                m_once_handler_pool = new();

            internal EventId On(A arg, Action<E, A> handler)
            {
                var id = AllocId();
                m_handlers.TryAdd(id, (handler, arg));
                return new(id, this);
            }

            internal EventId Once(A arg, Action<E, A> handler)
            {
                var id = AllocId();
                m_once_handlers.TryAdd(id, (handler, arg));
                return new(id, this);
            }

            internal override void Off(ulong id) => m_once_handlers.TryRemove(id, out _);

            internal override void Emit(E ev)
            {
                foreach (var (handler, arg) in m_handlers.Values)
                {
                    try
                    {
                        handler(ev, arg);
                    }
                    catch (Exception e)
                    {
                        WindowSystem.EmitUnhandledException(e);
                    }
                }

                if (m_once_handlers.Count <= 0) return;
                if (!m_once_handler_pool.TryDequeue(out var new_once))
                    new_once = new();
                var once = Interlocked.Exchange(ref m_once_handlers, new_once);

                foreach (var (handler, arg) in once.Values)
                {
                    try
                    {
                        handler(ev, arg);
                    }
                    catch (Exception e)
                    {
                        WindowSystem.EmitUnhandledException(e);
                    }
                }

                m_once_handler_pool.Enqueue(once);
            }
        }

        internal Class<A> GetOrAddClass<A>() => (Class<A>)m_classes.GetOrAdd(typeof(A), static _ => new Class<A>());

        internal void Emit(E ev)
        {
            foreach (var @class in m_classes.Values)
            {
                @class.Emit(ev);
            }
        }
    }

    internal Bus<E> GetOrAddBus<E>() => (Bus<E>)m_events.GetOrAdd(typeof(E), static _ => new Bus<E>());
    internal Bus<E>? TryGetBus<E>() => m_events.TryGetValue(typeof(E), out var bus) ? (Bus<E>)bus : null;

    #endregion

    #region On

    public EventId On<E>(Action<E> handler) => On<E, Action<E>>(handler, static (ev, handler) => handler(ev));

    public EventId Once<E>(Action<E> handler) => Once<E, Action<E>>(handler, static (ev, handler) => handler(ev));

    public EventId On<E, A>(A arg, Action<E, A> handler) => GetOrAddBus<E>().GetOrAddClass<A>().On(arg, handler);

    public EventId Once<E, A>(A arg, Action<E, A> handler) => GetOrAddBus<E>().GetOrAddClass<A>().Once(arg, handler);

    #endregion

    #region Emit

    public void Emit<E>(E ev) => TryGetBus<E>()?.Emit(ev);

    #endregion
}

#endregion
