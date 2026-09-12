using System;
using System.IO;
using Playdeck.Core;
namespace Playdeck;
public static class AssetVault {
 public static void PreserveCover(string root,Game game){if(!File.Exists(game.CoverPath))return;var directory=Path.Combine(root,"artwork");if(Discovery.Within(game.CoverPath,directory))return;Directory.CreateDirectory(directory);string path=Path.Combine(directory,game.Id+"-retained"+Path.GetExtension(game.CoverPath));File.Copy(game.CoverPath,path,true);game.CoverPath=path;}
}
