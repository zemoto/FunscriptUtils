using System.Text.Json.Serialization;
using ZemotoCommon.UI;

namespace VlcScriptPlayer.Config;

internal sealed class HandyConfig : ViewModelBase
{
   private bool _isConnected;
   [JsonIgnore]
   public bool IsConnected
   {
      get => _isConnected;
      set => SetProperty( ref _isConnected, value );
   }

   private string _connectionId;
   public string ConnectionId
   {
      get => _connectionId;
      set => SetProperty( ref _connectionId, value );
   }

   private int _desiredOffset = -125;
   public int DesiredOffset
   {
      get => _desiredOffset;
      set => SetProperty( ref _desiredOffset, value );
   }

   private int _currentOffset;
   [JsonIgnore]
   public int CurrentOffset
   {
      get => _currentOffset;
      set => SetProperty( ref _currentOffset, value );
   }
}
