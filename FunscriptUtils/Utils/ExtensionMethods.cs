using System;

namespace FunscriptUtils.Utils
{
   internal static class ExtensionMethods
   {
      public static bool RelativelyEqual( this long left, long right ) => Math.Abs( left - right ) < 0.05 * left;
      public static bool RelativelyEqual( this double left, double right ) => Math.Abs( left - right ) < 0.05 * left;
      public static string ToDisplayTime( this TimeSpan timeSpan ) => timeSpan.ToString( "mm\\:ss\\.FFF" );
   }
}