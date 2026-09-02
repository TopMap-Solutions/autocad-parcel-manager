using Autodesk.AutoCAD.Runtime;


[assembly: ExtensionApplication(typeof(ParcelManager.PluginExtension))]

[assembly: CommandClass(typeof(ParcelManger.Commands.ParcelCommands))]

[assembly: CommandClass(typeof(ParcelManger.Commands.GeoLocationCommands))]

[assembly: CommandClass(typeof(ParcelManager.Commands.BuddyCommands))]


namespace ParcelManager
{
    public class PluginExtension : IExtensionApplication
    {
        public void Initialize()
        {
            // Plugin startup
        }

        public void Terminate()
        {
            // Plugin shutdown
        }
    }
}