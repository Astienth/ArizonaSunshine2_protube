using MelonLoader;
using System.Text;
using HarmonyLib;
using Il2CppVertigo.AZS2.Client;
using Newtonsoft.Json;

namespace ArizonaSunshine2_protube
{
    public class ArizonaSunshine2_protube : MelonMod
    {
        public static string configPath = Directory.GetCurrentDirectory() + "\\Mods\\dualwield\\";
        public static bool dualWield = false;
        private MelonPreferences_Category config;
        public static bool leftHanded = false;

        public override void OnApplicationStart()
        {
            config = MelonPreferences.CreateCategory("provolver");
            config.CreateEntry<bool>("leftHanded", false);
            config.SetFilePath("Mods/Provolver/Provolver_config.cfg");
            leftHanded = bool.Parse(config.GetEntry("leftHanded").GetValueAsString());
            InitializeProTube();
        }

        public static void saveChannel(string channelName, string proTubeName)
        {
            string fileName = configPath + channelName + ".pro";
            File.WriteAllText(fileName, proTubeName, Encoding.UTF8);
        }

        public static string readChannel(string channelName)
        {
            string fileName = configPath + channelName + ".pro";
            if (!File.Exists(fileName)) return "";
            return File.ReadAllText(fileName, Encoding.UTF8);
        }

        public static void dualWieldSort()
        {
            ForceTubeVRInterface.FTChannelFile myChannels = JsonConvert.DeserializeObject<ForceTubeVRInterface.FTChannelFile>(ForceTubeVRInterface.ListChannels());
            var pistol1 = myChannels.channels.pistol1;
            var pistol2 = myChannels.channels.pistol2;
            if ((pistol1.Count > 0) && (pistol2.Count > 0))
            {
                dualWield = true;
                MelonLogger.Msg("Two ProTube devices detected, player is dual wielding.");
                if ((readChannel("rightHand") == "") || (readChannel("leftHand") == ""))
                {
                    MelonLogger.Msg("No configuration files found, saving current right and left hand pistols.");
                    saveChannel("rightHand", pistol1[0].name);
                    saveChannel("leftHand", pistol2[0].name);
                }
                else
                {
                    string rightHand = readChannel("rightHand");
                    string leftHand = readChannel("leftHand");
                    MelonLogger.Msg("Found and loaded configuration. Right hand: " + rightHand + ", Left hand: " + leftHand);
                    // Channels 4 and 5 are ForceTubeVRChannel.pistol1 and pistol2
                    ForceTubeVRInterface.ClearChannel(4);
                    ForceTubeVRInterface.ClearChannel(5);
                    ForceTubeVRInterface.AddToChannel(4, rightHand);
                    ForceTubeVRInterface.AddToChannel(5, leftHand);
                }
            }
            else
            {
                MelonLogger.Msg("SINGLE WIELD");
            }
        }
        private async void InitializeProTube()
        {
            MelonLogger.Msg("Initializing ProTube gear...");
            await ForceTubeVRInterface.InitAsync(true);
            Thread.Sleep(10000);
            dualWieldSort();
        }
        public static void shootProtube(string weaponType, bool isRightHand)
        {
            ForceTubeVRChannel channel = ForceTubeVRChannel.pistol1;
            if (isRightHand)
            {
                channel = (leftHanded && !dualWield) ? ForceTubeVRChannel.pistol2 : ForceTubeVRChannel.pistol1;
            }
            else
            {
                channel = (leftHanded && !dualWield) ? ForceTubeVRChannel.pistol1 : ForceTubeVRChannel.pistol2;
            }

            if (weaponType == "Shotgun")
            {
                ForceTubeVRInterface.Shoot(255, 125, 20f, channel);
                return;
            }
            if (weaponType == "Pistol")
            {
                ForceTubeVRInterface.Kick(210, channel);
                return;
            }
        }


        [HarmonyPatch(typeof(ProjectileShootStrategyBehaviourData), "PlayShootHapticsForHand", new Type[] { typeof(AZS2Hand) })]
        public class bhaptics_Recoil
        {
            [HarmonyPostfix]
            public static void Postfix(ProjectileShootStrategyBehaviourData __instance, AZS2Hand hand)
            {
                string weapon = "Pistol";
                if (__instance.shootStrategy.projectilesPerBurst > 1) weapon = "Shotgun";
                bool isRightHand = (hand.IsRightHand);
                shootProtube(weapon, isRightHand);
            }
        }
    }
}
