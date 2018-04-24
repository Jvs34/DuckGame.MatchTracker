using Harmony;
using DuckGame;

namespace MatchRecorder
{
    [HarmonyPatch( typeof( ModLoader ) )]
    [HarmonyPatch( "<GetModHash>b__4" )]
    [HarmonyPatch( new[] { typeof( DuckGame.Mod ) } )]
    class ModLoader_GetModHash
    {
        static bool Prefix( ref bool __result, DuckGame.Mod a )
        {
            if ( a is MatchRecorder.Mod )
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
