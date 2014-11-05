using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Robotics.PhysicalModel;
using W3C.Soap;

namespace Brumba.DwaNavigator
{
    public sealed class Contract
    {
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2014/11/dwanavigatorservice.html";
    }

    [DataContract]
    public class DwaNavigatorState
    {
        [DataMember]
        public VelocityAcceleration CurrentVelocityAcceleration { get; set; }

        [DataMember]
        public double[,] VelocititesEvaluation { get; set; }

        [DataMember]
        public Velocity VelocityMax { get; set; }

        [DataMember]
        public Vector2 Target { get; set; }

        [DataMember]
        double WheelAngularAccelerationMax { get; set; }
        
        [DataMember]
        double WheelAngularVelocityMax { get; set; }

        [DataMember]
        double WheelRadius { get; set; }

        [DataMember]
        double WheelBase { get; set; }

        [DataMember]
        double RobotRadius { get; set; }

        [DataMember]
        double RangefinderMaxRange { get; set; }

        [DataMember]
        public float DeltaT { get; set; }
    }

    [ServicePort]
	public class DwaNavigatorOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    public class Get : Get<GetRequestType, PortSet<DwaNavigatorState, Fault>>
    {
        public Get()
        {
        }

        public Get(GetRequestType body)
            : base(body)
        {
        }

        public Get(GetRequestType body, PortSet<DwaNavigatorState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    //[DataContract]
    //public class PoseRequest
    //{}

    //public class QueryPose : Query<PoseRequest, PortSet<Pose, DefaultQueryResponseType>>
    //{}

    //[DataContract]
    //public class InitPoseRequest
    //{
    //    [DataMember, DataMemberConstructor]
    //    public Pose Pose { get; set; }
    //}

    //public class InitPose : Update<InitPoseRequest, PortSet<DefaultUpdateResponseType, Fault>>
    //{}

    //[DataContract]
    //public class InitPoseUnknownRequest
    //{}

    //public class InitPoseUnknown : Update<InitPoseUnknownRequest, PortSet<DefaultUpdateResponseType, Fault>>
    //{}

    //[DataContract]
    //public class SubscribeRequest : SubscribeRequestType
    //{
    //}

    //public class Subscribe : Subscribe<SubscribeRequest, PortSet<SubscribeResponseType, Fault>>
    //{
    //    public Subscribe()
    //    {
    //    }

    //    public Subscribe(SubscribeRequest body)
    //        : base(body)
    //    {
    //    }

    //    public Subscribe(SubscribeRequest body, PortSet<SubscribeResponseType, Fault> responsePort)
    //        : base(body, responsePort)
    //    {
    //    }
    //}
}