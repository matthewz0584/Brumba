using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using System.Resources;
using System.Reflection;
using System.Globalization;
using Process = EnvDTE.Process;

namespace StartDssAddIn
{
	/// <summary>The object for implementing an Add-in.</summary>
	/// <seealso class='IDTExtensibility2' />
	public class Connect : IDTExtensibility2, IDTCommandTarget
	{
        private DTE2 m_applicationObject;
        private AddIn m_addInInstance;
	    private const string MANIFEST_EXTENSION = "manifest.xml";
	    private const string DSS_EXE_PATH = @"C:\MRDS4\bin\DssHost32.exe";
        private const string SECURITY_FILE_PATH = @"C:\MRDS4\brumba\config\DssHost32.security";
		/// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
		public Connect()
		{
		}

		/// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
            m_applicationObject = (DTE2)application;

            m_addInInstance = (AddIn)addInInst;
            if (connectMode == ext_ConnectMode.ext_cm_UISetup)
            {
                var contextGUIDS = new object[] { };
                var commands = (Commands2)m_applicationObject.Commands;
                CommandBar standardToolBar = ((CommandBars)m_applicationObject.CommandBars)["Item"];

                try
                {
                    Command command = commands.AddNamedCommand2(m_addInInstance, "StartDssAddIn", "Start dss", "Executes the command for StartDssAddIn", true, 59, ref contextGUIDS);
                    if ((command != null) && (standardToolBar != null))
                    {   
                        var ctrl = (CommandBarControl)command.AddControl(standardToolBar, 1);
                        ctrl.TooltipText = "Starts selected manifest";
                    }
                }
                catch (System.ArgumentException)
                {
                }
            }
		}

		/// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
		}

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
		public void OnAddInsUpdate(ref Array custom)
		{
		}

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
		}

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
		}
		
		/// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
		/// <param term='commandName'>The name of the command to determine state for.</param>
		/// <param term='neededText'>Text that is needed for the command.</param>
		/// <param term='status'>The state of the command in the user interface.</param>
		/// <param term='commandText'>Text requested by the neededText parameter.</param>
		/// <seealso class='Exec' />
		public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
		{
			if(neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
			{
				if(commandName == "StartDssAddIn.Connect.StartDssAddIn")
				{
                    if (!GetSelectedItemInfo().FullName.ToLower().EndsWith(MANIFEST_EXTENSION))
                    {
                        status = vsCommandStatus.vsCommandStatusInvisible;
                        return;
                    }
				    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported|vsCommandStatus.vsCommandStatusEnabled;					
				}
			}
		}

		/// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
		/// <param term='commandName'>The name of the command to execute.</param>
		/// <param term='executeOption'>Describes how the command should be run.</param>
		/// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
		/// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
		/// <param term='handled'>Informs the caller if the command was handled or not.</param>
		/// <seealso class='Exec' />
		public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
		{           
            handled = false;
			if(executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
			{
				if(commandName == "StartDssAddIn.Connect.StartDssAddIn")
				{
				    StartManifest();
					handled = true;
					return;
				}
			}
		}

	    private void StartManifest()
	    {
	        var selectedManifest = GetSelectedItemInfo();
            var startInfo = new ProcessStartInfo
            {
                FileName = DSS_EXE_PATH,
                Arguments = "/port:50000 /tcpport:50001 /manifest:" + selectedManifest.FullName + " /s:" + SECURITY_FILE_PATH
            };
	        System.Diagnostics.Process.Start(startInfo);
	    }

	    private FileInfo GetSelectedItemInfo()
	    {
            var uih = m_applicationObject.ToolWindows.SolutionExplorer;
            var selectedItems = ((Array)uih.SelectedItems);
            var item = selectedItems.GetValue(0) as UIHierarchyItem;
            var projItem = item.Object as ProjectItem;
            return new FileInfo(projItem.Properties.Item("FullPath").Value.ToString());
	    }

	}
}