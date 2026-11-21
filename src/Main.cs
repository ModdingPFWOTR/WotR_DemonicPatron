using System;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using UnityModManagerNet;

namespace DemonicPatron
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry;
        public static Harmony HarmonyInstance;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            HarmonyInstance = new Harmony(modEntry.Info.Id);
            modEntry.OnGUI = OnGUI;

            try
            {
                HarmonyInstance.PatchAll();
                Log("Demonic Patron loaded.");
            }
            catch (Exception ex)
            {
                Log("Harmony patch failed: " + ex);
                return false;
            }

            return true;
        }

        public static void Log(string message)
        {
            ModEntry?.Logger.Log(message);
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UnityEngine.GUILayout.Label("Adds the Demonic Patron rogue archetype, based on Eldritch Scoundrel.");
            UnityEngine.GUILayout.Label("Danger Sense and Finesse Training are removed. Weapon Finesse remains.");
            UnityEngine.GUILayout.Label("At 3rd level, choose Flauros, Sithhud, Pazuzu, or Aldinach. The patron has its own progression.");
            UnityEngine.GUILayout.Label("Create a new rogue and choose Demonic Patron.");
        }
    }

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    static class BlueprintsReadyPatch
    {
        static void Postfix()
        {
            try
            {
                Content.Create();
            }
            catch (Exception ex)
            {
                Main.Log("Demonic Patron setup failed: " + ex);
            }
        }
    }
}
