using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Brumba.DebugDssManifestVsPackage
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    //Guid VSConstants.UICONTEXT_SolutionExists
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    [Guid(GuidList.guidDebugDssManifestVsPackagePkgString)]
    public sealed class DebugDssManifestVsPackage : Package
    {
        const string MANIFEST_EXTENSION = ".manifest.xml";
        const string DSS_HOST_PATH = @"C:\MRDS4\bin\DssHost32.exe";
        const string DSS_HOST_SECURITY_FILE_PATH = @"C:\MRDS4\brumba\config\DssHost32.security";
        const string DSS_HOST_ARGS = @"/port:50000 /tcpport:50001 /manifest:{0} /s:{1}";

        string _selectedManifest;
        IVsDebugger _debugger;
        DTE2 _ide;

        protected override void Initialize()
        {
            base.Initialize();

            _debugger = GetService(typeof(IVsDebugger)) as IVsDebugger;
            _ide = (DTE2)GetService(typeof(DTE));
            
            var menuCommandID = new CommandID(GuidList.guidDebugDssManifestVsPackageCmdSet, (int)PkgCmdIDList.cmdidMyCommand);
            var menuItem = new OleMenuCommand((_, __) => DebugManifest(), menuCommandID);
            menuItem.BeforeQueryStatus += MenuItemOnBeforeQueryStatus;
            (GetService(typeof(IMenuCommandService)) as OleMenuCommandService).AddCommand(menuItem);
        }

        private void MenuItemOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {            
            var selectedItems = (_ide.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object as UIHierarchy).SelectedItems as UIHierarchyItem[];

            var itemIsManifest = selectedItems.Length == 1 && selectedItems[0].Name.ToLower().EndsWith(MANIFEST_EXTENSION);
            (sender as OleMenuCommand).Visible = itemIsManifest;

            if (itemIsManifest)           
                _selectedManifest = (selectedItems[0].Object as ProjectItem).Properties.Item("FullPath").Value as string;
        }

        private void DebugManifest()
        {
            var info = new VsDebugTargetInfo
            {
                bstrExe = DSS_HOST_PATH,
                bstrArg = string.Format(DSS_HOST_ARGS, _selectedManifest, DSS_HOST_SECURITY_FILE_PATH),
                dlo = DEBUG_LAUNCH_OPERATION.DLO_CreateProcess,
                clsidCustom = VSConstants.CLSID_ComPlusOnlyDebugEngine //Set the managed debugger
                //clsidCustom = new Guid("{3B476D35-A401-11D2-AAD4-00C04F990171}") //Set the unmanged debugger
            };
            info.cbSize = (uint) Marshal.SizeOf(info);

            var ptr = Marshal.AllocCoTaskMem((int) info.cbSize);
            try
            {
                Marshal.StructureToPtr(info, ptr, false);

                var res = _debugger.LaunchDebugTargets(1, ptr);
                if (res != VSConstants.S_OK)
                    (GetService(typeof (IVsUIShell)) as IVsUIShell).ReportErrorInfo(res);
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }
    }
}
