using System.Collections.Generic;
using System.ComponentModel;
using Brumba.Utils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Services.Drive.Proxy;

namespace Brumba.WaiterStupid.Odometry
{
	[Contract(Contract.Identifier)]
	[DisplayName("Brumba diff drive odometry service")]
	[Description("no description provided")]
	public class OdometryService : DsspServiceBase
	{
		[ServiceState]
		OdometryState _state = new OdometryState();

		[ServicePort("/Odometry", AllowMultipleInstances = true)]
		OdometryOperations _mainPort = new OdometryOperations();

		[Partner("DiffDrive", Contract = Microsoft.Robotics.Services.Drive.Proxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExisting)]
		DriveOperations _diffDrive = new DriveOperations();

	    readonly OdometryCalculator _odometryCalc;

		public OdometryService(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
			_odometryCalc = new OdometryCalculator();
		}

		protected override void Start()
		{
		    base.Start();

			//1. Разобраться со временем: симулированным или нет
			//2. Разобраться с установкой констант для сервиса: делта т и константы
			//try
			//{
			//	ServiceForwarder<SimulatedTimerOperations>(String.Format(@"{0}://{1}/{2}", ServiceInfo.HttpServiceAlias.Scheme, ServiceInfo.HttpServiceAlias.Authority, "SimulatedTimer"));
			//}
			//catch (Exception)
			//{

			//}
			

		    SpawnIterator(Execute);
		}

		IEnumerator<ITask> Execute()
		{
			DriveDifferentialTwoWheelState driveState = null;
			yield return Arbiter.Receive(false, _diffDrive.Get(), (DriveDifferentialTwoWheelState ds) => driveState = ds);
			_state.LeftTicks = driveState.LeftWheel.EncoderState.CurrentReading;
			_state.RightTicks = driveState.RightWheel.EncoderState.CurrentReading;

			while (true)
			{
				yield return To.Exec(TimeoutPort(100));

				yield return Arbiter.Receive(false, _diffDrive.Get(), (DriveDifferentialTwoWheelState ds) => driveState = ds);

				_state = _odometryCalc.UpdateOdometry(_state, 100, driveState.LeftWheel.EncoderState.CurrentReading, driveState.RightWheel.EncoderState.CurrentReading);
			}
		}
	}
}
