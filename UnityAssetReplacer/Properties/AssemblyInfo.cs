using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("UnityAssetReplacer")]
[assembly: AssemblyDescription("Program to dump and/or replace Unity Assets from a Unity Asset Bundle.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("Luke Simmons")]
[assembly: AssemblyProduct("UnityAssetReplacer")]
[assembly: AssemblyCopyright("Copyright Luke Simmons © 2021")]
[assembly: AssemblyTrademark("Luke Simmons")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f128fdc3-975c-4d1d-b258-9cde2a80c92d")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.1.2.0")]
[assembly: AssemblyFileVersion("2.1.2.0")]
[assembly: NeutralResourcesLanguage("en-US")]
