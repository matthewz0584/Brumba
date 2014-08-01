using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Ccr.Core;
using W3C.Soap;
using DC = System.Diagnostics.Contracts;

namespace Brumba.DsspUtils
{
    public static class To
    {
        [DC.ContractAbbreviator]
        [Conditional("CONTRACTS_FULL")]
        static void AssertActionCall(object call)
        {
            DC.Contract.Requires(call != null);
        }

        class ActionCall
        {
            readonly Func<IEnumerator<ITask>> _call;

            public ActionCall(Func<IEnumerator<ITask>> call)
            {
                AssertActionCall(call);

                _call = call;
            }

            public IEnumerator<ITask> Call()
            {
                return _call();
            }
        }

        class ActionCall<T1>
        {
            readonly Func<T1, IEnumerator<ITask>> _call;
            readonly T1 _param1;

            public ActionCall(Func<T1, IEnumerator<ITask>> call, T1 param1)
            {
                AssertActionCall(call);

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
            readonly Func<T1, T2, IEnumerator<ITask>> _call;
            readonly T1 _param1;
            readonly T2 _param2;

            public ActionCall(Func<T1, T2, IEnumerator<ITask>> call, T1 param1, T2 param2)
            {
                AssertActionCall(call);

                _call = call;
                _param1 = param1;
                _param2 = param2;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_param1, _param2);
            }
        }

        class ActionCall<T1, T2, T3>
        {
            readonly Func<T1, T2, T3, IEnumerator<ITask>> _call;
            readonly T1 _param1;
            readonly T2 _param2;
            readonly T3 _param3;

            public ActionCall(Func<T1, T2, T3, IEnumerator<ITask>> call, T1 param1, T2 param2, T3 param3)
            {
                AssertActionCall(call);

                _call = call;
                _param1 = param1;
                _param2 = param2;
                _param3 = param3;
            }

            public IEnumerator<ITask> Call()
            {
                return _call(_param1, _param2, _param3);
            }
        }

        [DC.ContractAbbreviator]
        [Conditional("CONTRACTS_FULL")]
        static void AssertFuncCall<TRet>(object call, Action<TRet> @return)
        {
            DC.Contract.Requires(call != null);
            DC.Contract.Requires(@return != null);
        }

        class FuncCall<TRet>
        {
            readonly Func<Action<TRet>, IEnumerator<ITask>> _call;
            readonly Action<TRet> _return;

            public FuncCall(Func<Action<TRet>, IEnumerator<ITask>> call, Action<TRet> @return)
            {
                AssertFuncCall(call, @return);

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
            readonly Func<Action<TRet>, T1, IEnumerator<ITask>> _call;
            readonly T1 _param1;
            readonly Action<TRet> _return;

            public FuncCall(Func<Action<TRet>, T1, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1)
            {
                AssertFuncCall(call, @return);

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
            readonly Func<Action<TRet>, T1, T2, IEnumerator<ITask>> _call;
            readonly T1 _param1;
            readonly T2 _param2;
            readonly Action<TRet> _return;

            public FuncCall(Func<Action<TRet>, T1, T2, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1, T2 param2)
            {
                AssertFuncCall(call, @return);

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
            readonly Func<Action<TRet>, T1, T2, T3, IEnumerator<ITask>> _call;
            readonly T1 _param1;
            readonly T2 _param2;
            readonly T3 _param3;
            readonly Action<TRet> _return;

            public FuncCall(Func<Action<TRet>, T1, T2, T3, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1, T2 param2, T3 param3)
            {
                AssertFuncCall(call, @return);

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

        [DC.ContractAbbreviator]
        static void AssertExecReturnContract<TRet>(object call, Action<TRet> @return)
        {
            DC.Contract.Requires(call != null);
            DC.Contract.Requires(@return != null);
            DC.Contract.Ensures(DC.Contract.Result<ITask>() != null);
        }

        [DC.ContractAbbreviator]
        static void AssertExecContract(object call)
        {
            DC.Contract.Requires(call != null);
            DC.Contract.Ensures(DC.Contract.Result<ITask>() != null);
        }

        public static ITask Exec<T1, T2, TRet>(Func<Action<TRet>, T1, T2, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1, T2 param2)
        {
            AssertExecReturnContract(call, @return);

            return Arbiter.FromIteratorHandler(new FuncCall<T1, T2, TRet>(call, @return, param1, param2).Call);
        }

        public static ITask Exec<T1, T2, T3, TRet>(Func<Action<TRet>, T1, T2, T3, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1, T2 param2, T3 param3)
        {
            AssertExecReturnContract(call, @return);

            return Arbiter.FromIteratorHandler(new FuncCall<T1, T2, T3, TRet>(call, @return, param1, param2, param3).Call);
        }

        public static ITask Exec<T1, TRet>(Func<Action<TRet>, T1, IEnumerator<ITask>> call, Action<TRet> @return, T1 param1)
        {
            AssertExecReturnContract(call, @return);

            return Arbiter.FromIteratorHandler(new FuncCall<T1, TRet>(call, @return, param1).Call);
        }

        public static ITask Exec<TRet>(Func<Action<TRet>, IEnumerator<ITask>> call, Action<TRet> @return)
        {
            AssertExecReturnContract(call, @return);

            return Arbiter.FromIteratorHandler(new FuncCall<TRet>(call, @return).Call);
        }

        public static ITask Exec<T1, T2, T3>(Func<T1, T2, T3, IEnumerator<ITask>> call, T1 param1, T2 param2, T3 param3)
        {
            AssertExecContract(call);

            return Arbiter.FromIteratorHandler(new ActionCall<T1, T2, T3>(call, param1, param2, param3).Call);
        }

        public static ITask Exec<T1, T2>(Func<T1, T2, IEnumerator<ITask>> call, T1 param1, T2 param2)
        {
            AssertExecContract(call);

            return Arbiter.FromIteratorHandler(new ActionCall<T1, T2>(call, param1, param2).Call);
        }

        public static ITask Exec<T1>(Func<T1, IEnumerator<ITask>> call, T1 param1)
        {
            AssertExecContract(call);

            return Arbiter.FromIteratorHandler(new ActionCall<T1>(call, param1).Call);
        }

        public static ITask Exec(Func<IEnumerator<ITask>> call)
        {
            AssertExecContract(call);

            return Arbiter.FromIteratorHandler(new ActionCall(call).Call);
        }

        public static ITask Exec<T1, T2>(PortSet<T1, T2> portSet)
            where T2 : Fault
        {
            AssertExecContract(portSet);

            return Arbiter.Choice(portSet, p1 => { }, p2 =>
                {
                    Console.WriteLine("Choice on {0} returned error {1}", portSet, p2);
                    throw p2.ToException();
                });
        }
    }
}
