using System.Collections.Generic;

namespace VlcScriptPlayer;

internal sealed class Config
{
   public string ConnectionId { get; set; }
   public int DesiredOffset { get; set; }
   public List<string> ScriptFolders { get; set; } = new();
}