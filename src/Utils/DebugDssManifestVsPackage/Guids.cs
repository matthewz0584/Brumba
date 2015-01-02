// Guids.cs
// MUST match guids.h

using System;

namespace Brumba.DebugDssManifestVsPackage
{
    static class GuidList
    {
        public const string guidDebugDssManifestVsPackagePkgString = "6023b47f-47dc-41a3-ac05-b1dc2f375428";
        public const string guidDebugDssManifestVsPackageCmdSetString = "0b33bcb6-32e5-4d92-957d-d7ee74edf6f4";

        public static readonly Guid guidDebugDssManifestVsPackageCmdSet = new Guid(guidDebugDssManifestVsPackageCmdSetString);
    };
}