using System.Diagnostics;
namespace Playdeck.Core;
public static class ToolLaunch {
 public static int Start(Game tool){
  if(!File.Exists(tool.LaunchPath))throw new FileNotFoundException("The tool's launch file is missing.",tool.LaunchPath);
  var info=new ProcessStartInfo(tool.LaunchPath){UseShellExecute=true};if(Path.GetExtension(tool.LaunchPath).Equals(".exe",StringComparison.OrdinalIgnoreCase))info.Arguments=tool.Arguments;if(Directory.Exists(tool.WorkingDirectory))info.WorkingDirectory=tool.WorkingDirectory;
  using var process=Process.Start(info);return process?.Id??0;
 }
}
