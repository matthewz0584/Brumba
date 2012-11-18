using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Ccr.Core;
using W3C.Soap;

namespace Brumba.Simulation.SimulationTester
{
    public class To
    {
        class ParamCall
        {
            Func<IEnumerator<ITask>> _call;

            public ParamCall(Func<IEnumerator<ITask>> call)
            {
                _call = call;
            }

            public IEnumerator<ITask> Call()
            {
                return _call();
            }
        }

        class ParamCall<TRet>
        {
            Func<Action<TRet>, IEnumerator<ITask>> _call;
            Action<TRet> _return;

            public ParamCall(Func<Action<TRet>, IEnumerator<ITask>> call, Action<TRet> @return)
            {
                _call = call;
                _return = @return;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_return);
            }
        }

        class ParamCall<T1, TRet>
        {
            Func<Action<TRet>, T1, IEnumerator<ITask>> _call;
            T1 _param1;
            Action<TRet> _return;

            public ParamCall(Func<Action<TRet>, T1, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1)
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

        public static ITask Exec<T1, TRet>(Func<Action<TRet>, T1, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1)
        {
            return Arbiter.FromIteratorHandler(new ParamCall<T1, TRet>(call, @return, param1).Call);
        }

        public static ITask Exec<TRet>(Func<Action<TRet>, IEnumerator<ITask>> call, Action<TRet> @return)
        {
            return Arbiter.FromIteratorHandler(new ParamCall<TRet>(call, @return).Call);
        }

        public static ITask Exec(Func<IEnumerator<ITask>> call)
        {
            return Arbiter.FromIteratorHandler(new ParamCall(call).Call);
        }

        public static ITask Exec<T1>(Port<T1> portSet)
        {
            return Arbiter.Receive<T1>(false, portSet, (T1 val) => { });
        }

        public static ITask Exec<T1, T2>(PortSet<T1, T2> portSet)
        {
            return Arbiter.Choice(portSet, (T1 p1) => { }, (T2 p2) => { });
        }
    }
}
