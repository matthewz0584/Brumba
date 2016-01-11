using System.Diagnostics.Contracts;

namespace Brumba.Utils
{
    [ContractClass(typeof(IFreezableContract))]
    public interface IFreezable
    {
        void Freeze();
        bool Freezed { get; }
    }

    [ContractClassForAttribute(typeof(IFreezable))]
    abstract class IFreezableContract : IFreezable
    {
        public void Freeze()
        {
            Contract.Ensures(Freezed);
        }

        public bool Freezed { get; private set; }
    }
}