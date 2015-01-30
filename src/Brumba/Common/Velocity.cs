using System;
using Brumba.Utils;
using Microsoft.Dss.Core.Attributes;
using DC = System.Diagnostics.Contracts;

namespace Brumba.Common
{
    [DataContract]
    public struct Velocity : IFreezable
    {
        double _linear;
        double _angular;

        public Velocity(double linear, double angular)
        {
            _linear = linear;
            _angular = angular;

            _freezed = true;
        }

        [DataMember, DataMemberConstructor]
        public double Linear
        {
            get { return _linear; }
            set { DC.Contract.Requires(!Freezed); _linear = value; }
        }

        [DataMember, DataMemberConstructor]
        public double Angular
        {
            get { return _angular; }
            set { DC.Contract.Requires(!Freezed); _angular = value; }
        }

        public bool IsRectilinear
        {
            get { return Math.Abs(Angular) <= 0.01; }
        }

        public override string ToString()
        {
            return string.Format("(L:{0}, A:{1})", Linear, Angular);
        }

        bool _freezed;

        public void Freeze()
        {
            _freezed = true;
        }

        public bool Freezed
        {
            get { return _freezed; }
        }
    }
}