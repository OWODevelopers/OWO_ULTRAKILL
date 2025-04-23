using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWO_ULTRAKILL
{
    [BepInPlugin("org.bepinex.plugins.OWO_ULTRAKILL", "OWO_ULTRAKILL", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {

#pragma warning disable CS0109
        internal static new ManualLogSource Log;
#pragma warning restore CS0109

        public static OWOSkin owoSkin;


        private void Awake()
        {
            Log = Logger;
            Logger.LogMessage("OWO_ULTRAKILL plugin is loaded!");

            owoSkin = new OWOSkin();


            var harmony = new Harmony("owo.patch.ultrakill");
            harmony.PatchAll();
        }

    }
}
