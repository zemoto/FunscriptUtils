using Newtonsoft.Json;
using System.IO;

namespace FunscriptUtils.Generating
{
   internal sealed class GenerationParams
   {
      public static GenerationParams FromFile( string filePath ) => JsonConvert.DeserializeObject<GenerationParams>( File.ReadAllText( filePath ) );

      public string VideoFilePath { get; set; }
      public string GenerationTemplateFilePath { get; set; }
      public int TemplateThreshold { get; set; } = 128;
      public int TemplateOffset { get; set; } = 0;
   }
}
