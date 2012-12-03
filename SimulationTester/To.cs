using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Ccr.Core;
using W3C.Soap;

namespace Brumba.Simulation.SimulationTester
{
    public static class To
    {
        public static List<T> Addd<T>(this List<T> me, T toAdd)
        {
            me.Add(toAdd);
            return me;
        }

        class ActionCall
        {
            Func<IEnumerator<ITask>> _call;

            public ActionCall(Func<IEnumerator<ITask>> call)
            {
                _call = call;
            }

            public IEnumerator<ITask> Call()
            {
                return _call();
            }
        }

        class ActionCall<T1>
        {
            Func<T1, IEnumerator<ITask>> _call;
            T1 _param1;

            public ActionCall(Func<T1, IEnumerator<ITask>> call, T1 param1)
            {
                _call = call;
                _param1 = param1;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_param1);
            }
        }

        class ActionCall<T1, T2>
        {
            Func<T1, T2, IEnumerator<ITask>> _call;
            T1 _param1;
            T2 _param2;

            public ActionCall(Func<T1, T2, IEnumerator<ITask>> call, T1 param1, T2 param2)
            {
                _call = call;
                _param1 = param1;
                _param2 = param2;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_param1, _param2);
            }
        }

        class FuncCall<TRet>
        {
            Func<Action<TRet>, IEnumerator<ITask>> _call;
            Action<TRet> _return;

            public FuncCall(Func<Action<TRet>, IEnumerator<ITask>> call, Action<TRet> @return)
            {
                _call = call;
                _return = @return;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_return);
            }
        }

        class FuncCall<T1, TRet>
        {
            Func<Action<TRet>, T1, IEnumerator<ITask>> _call;
            T1 _param1;
            Action<TRet> _return;

            public FuncCall(Func<Action<TRet>, T1, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1)
            {
                _call = call;
                _param1 = param1;
                _return = @return;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_return, _param1);
            }
        }

        class FuncCall<T1, T2, TRet>
        {
            Func<Action<TRet>, T1, T2, IEnumerator<ITask>> _call;
            T1 _param1;
            T2 _param2;
            Action<TRet> _return;

            public FuncCall(Func<Action<TRet>, T1, T2, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1, T2 param2)
            {
                _call = call;
                _param1 = param1;
                _param2 = param2;
                _return = @return;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_return, _param1, _param2);
            }
        }

        class FuncCall<T1, T2, T3, TRet>
        {
            Func<Action<TRet>, T1, T2, T3, IEnumerator<ITask>> _call;
            T1 _param1;
            T2 _param2;
            T3 _param3;
            Action<TRet> _return;

            public FuncCall(Func<Action<TRet>, T1, T2, T3, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1, T2 param2, T3 param3)
            {
                _call = call;
                _param1 = param1;
                _param2 = param2;
                _param3 = param3;
                _return = @return;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_return, _param1, _param2, _param3);
            }
        }

        public static ITask Exec<T1, T2, TRet>(Func<Action<TRet>, T1, T2, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1, T2 param2)
        {
            return Arbiter.FromIteratorHandler(new FuncCall<T1, T2, TRet>(call, @return, param1, param2).Call);
        }

        public static ITask Exec<T1, T2, T3, TRet>(Func<Action<TRet>, T1, T2, T3, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1, T2 param2, T3 param3)
        {
            return Arbiter.FromIteratorHandler(new FuncCall<T1, T2, T3, TRet>(call, @return, param1, param2, param3).Call);
        }

        public static ITask Exec<T1, TRet>(Func<Action<TRet>, T1, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1)
        {
            return Arbiter.FromIteratorHandler(new FuncCall<T1, TRet>(call, @return, param1).Call);
        }

        public static ITask Exec<TRet>(Func<Action<TRet>, IEnumerator<ITask>> call, Action<TRet> @return)
        {
            return Arbiter.FromIteratorHandler(new FuncCall<TRet>(call, @return).Call);
        }

        public static ITask Exec<T1, T2>(Func<T1, T2, IEnumerator<ITask>> call, T1 param1, T2 param2)
        {
            return Arbiter.FromIteratorHandler(new ActionCall<T1, T2>(call, param1, param2).Call);
        }

        public static ITask Exec<T1>(Func<T1, IEnumerator<ITask>> call, T1 param1)
        {
            return Arbiter.FromIteratorHandler(new ActionCall<T1>(call, param1).Call);
        }

        public static ITask Exec(Func<IEnumerator<ITask>> call)
        {
            return Arbiter.FromIteratorHandler(new ActionCall(call).Call);
        }

        public static ITask Exec<T1>(Port<T1> portSet)
        {
            return Arbiter.Receive<T1>(false, portSet, (T1 val) => { });
        }

        public static ITask Exec<T1, T2>(PortSet<T1, T2> portSet)
        {
            return Arbiter.Choice(portSet, (T1 p1) => { }, (T2 p2) => { Console.WriteLine("qqqqqqqqqqqqq"); });
        }
    }
}
