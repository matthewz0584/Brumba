using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Ccr.Core;
using W3C.Soap;

namespace Brumba.Simulation.SimulationTester
{
    public class RetVal<T>
    {
        public T V { get; set; }
    }

    public class ArbiterMy
    {
        class ParamCall<T1, TRet>
        {
            Func<T1, RetVal<TRet>, IEnumerator<ITask>> _call;
            T1 _param1;
            RetVal<TRet> _retVal;

            public ParamCall(Func<T1, RetVal<TRet>, IEnumerator<ITask>> call, T1 param1, RetVal<TRet> retVal)
            {
                _call = call;
                _param1 = param1;
                _retVal = retVal;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_param1, _retVal);
            }
        }

        public static ITask FromIteratorHandler<T1, TRet>(Func<T1, RetVal<TRet>, IEnumerator<ITask>> call, T1 param1, RetVal<TRet> retVal)
        {
            return Arbiter.FromIteratorHandler(new ParamCall<T1, TRet>(call, param1, retVal).Call);
        }

        public static ITask Exec<T>(PortSet<T, Fault> portSet)
        {
            return Arbiter.Receive<T>(false, portSet, (T val) => { });
        }

        public static ITask Receive<T1, T2>(PortSet<T1, T2> portSet, RetVal<T1> retVal = null)
        {
            return Arbiter.Receive<T1>(false, portSet, (T1 val) => { if (retVal != null) retVal.V = val; });
        }

        public static ITask ReceiveFaulty<T1>(PortSet<T1, Exception> portSet, RetVal<T1> retVal = null, Action<Exception> errorLogger = null)
        {
            return Arbiter.Choice<T1, Exception>(portSet,
                (T1 val) => { if (retVal != null) retVal.V = val; },
                (Exception f) => { retVal.V = default(T1); if (errorLogger != null) errorLogger(f); });
        }
    }
}
