//------------------------------------------------------------------------------
//  <copyright file="ReferencePlatform2011Types.cs" company="Microsoft Corporation">
//      Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//------------------------------------------------------------------------------

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace Brumba.Simulation.SimulatedReferencePlatform2011
{
    /// <summary>
    /// ReferencePlatform2011 contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// DSS contract identifier for ReferencePlatform2011
        /// </summary>
        [DataMember]
        public const string Identifier = "http://brumba.ru/contracts/2013/11/simulatedreferenceplatform2011.html";
    }

    /// <summary>
    /// ReferencePlatform2011 state
    /// </summary>
    [DataContract]
    public class ReferencePlatform2011State
    {
        /// <summary>
        /// Gets or sets the differential drive state
        /// </summary>
        [DataMember]
        public Microsoft.Robotics.Services.Drive.DriveDifferentialTwoWheelState DriveState { get; set; }
    }

    /// <summary>
    /// ReferencePlatform2011 main operations port
    /// </summary>
    [ServicePort]
    public class ReferencePlatform2011Operations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, Subscribe>
    {
    }

    /// <summary>
    /// ReferencePlatform2011 get operation
    /// </summary>
    public class Get : Get<GetRequestType, PortSet<ReferencePlatform2011State, Fault>>
    {
        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        public Get()
        {
        }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">The request message body</param>
        public Get(GetRequestType body)
            : base(body)
        {
        }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">The request message body</param>
        /// <param name="responsePort">The response port for the request</param>
        public Get(GetRequestType body, PortSet<ReferencePlatform2011State, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// ReferencePlatform2011 subscribe operation
    /// </summary>
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>>
    {
        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        public Subscribe()
        {
        }

        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        /// <param name="body">The request message body</param>
        public Subscribe(SubscribeRequestType body)
            : base(body)
        {
        }

        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        /// <param name="body">The request message body</param>
        /// <param name="responsePort">The response port for the request</param>
        public Subscribe(SubscribeRequestType body, PortSet<SubscribeResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
}
