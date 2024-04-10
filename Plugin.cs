using BepInEx;
using BepInEx.Configuration;
using UnityEngine.Rendering;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Voice.Unity;

namespace KnockOutWarning
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private GameObject myPlayerObject = null;
        private Player myPlayer;
        Recorder myAudioRecorder = null;
        GameObject myPostPlayerObject;
        GameObject myPostSleepObject;
        private bool attached;
        
        private static ConfigEntry<bool> configRagdollMuteMic { get; set; }
        private static ConfigEntry<bool> configRagdollTriggerByWorld { get; set; }
        private static ConfigEntry<bool> configRagdollTriggerByItems { get; set; }
        private static ConfigEntry<int> configRagdollItemBehaviour { get; set; }
        private static ConfigEntry<int> configRagdollBlurMode { get; set; }
        private static ConfigEntry<float> configRagdollTriggerForceWorld { get; set; }
        private static ConfigEntry<float> configRagdollTriggerForceItems { get; set; }
        private static ConfigEntry<float> configRagdollMinimumDuration { get; set; }
        private static ConfigEntry<float> configRagdollMaximumDuration { get; set; }


        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            configRagdollMuteMic = Config.Bind("General.Toggles", "RagdollMuteMic", true, "Dictates if the mic is switched off when being knocked out.");
            configRagdollTriggerByWorld = Config.Bind("General.Toggles", "RagdollTriggerByWorld", true, "Dictates if the ragdoll is triggered when being hit by terrain and static objects.");
            configRagdollTriggerByItems = Config.Bind("General.Toggles", "RagdollTriggerByItems", true, "Dictates if the ragdoll is triggered when being hit by items and dynamic objects.");
            configRagdollItemBehaviour = Config.Bind("General", "RagdollItemBehaviour", 2, "Dictates if held item is 0:kept in hand (may cause bugs), 1:droped, 2:unequiped when being knocked out.");
            configRagdollBlurMode = Config.Bind("General", "RagdollBlurMode", 2, "Dictates if the vision is 0:normal, 1:blurry, 2:blacked out when being knocked out.");
            configRagdollTriggerForceWorld = Config.Bind("General", "RagdollTriggerForceWorld", 250.0f, "The amount of force to trigger the ragdoll when hit by terrain and static objects.");
            configRagdollTriggerForceItems = Config.Bind("General", "RagdollForceTriggerItems", 25.0f, "The amount of force to trigger the ragdoll when hit by items and dynamic objetcs.");
            configRagdollMinimumDuration = Config.Bind("General", "RagdollMinimumDuration", 1.5f, "The minimum amount of time to trigger the ragdoll for.");
            configRagdollMaximumDuration = Config.Bind("General", "RagdollMAximumDuration", 6.0f, "The maximum amount of time to trigger the ragdoll for.");

        }

        private void Start()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} started!");
            SceneManager.sceneLoaded += SetupScene;
        }

        private void SetupScene(Scene scene, LoadSceneMode mode)
        {

            Logger.LogInfo($"Setting up mod for scene {scene.buildIndex}");

            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                attached = true;
            }
            else
            {
                attached = false;
            }
        }

        public void Update()
        {
            if (myPlayerObject == null && !attached)
            {
                ScanComponentsInScene();
            }

            if (myPostPlayerObject != null && myPostSleepObject != null && !attached)
            {
                PatchBlurRenderer();
            }

            if (myPlayerObject != null && !attached)
            {
                AttachKOHandler();
            }

        }

        private void AttachKOHandler()
        {
            Logger.LogInfo($"Attaching KOHandler to {myPlayerObject.GetInstanceID()}");

            if (myPlayerObject != null)
            {
                GameObject HeadPos = myPlayerObject.transform.Find("HeadPosition").gameObject;
                GameObject voice = HeadPos.transform.Find("Voice").gameObject;
                myAudioRecorder = voice.GetComponent<Recorder>();

                GameObject rigCreator = myPlayerObject.transform.Find("RigCreator").gameObject;
                GameObject rig = rigCreator.transform.Find("Rig").gameObject;
                GameObject armature = rig.transform.Find("Armature").gameObject;
                GameObject hip = armature.transform.Find("Hip").gameObject;
                GameObject torso = hip.transform.Find("Torso").gameObject;
                GameObject head = torso.transform.Find("Head").gameObject;
                myPlayer = myPlayerObject.GetComponent<Player>();

                KnockOutHandler koHandler = head.AddComponent<KnockOutHandler>();
                SphereCollider KOCollider = head.AddComponent<SphereCollider>();

                KOCollider.isTrigger = true;
                KOCollider.radius = 1;

                koHandler.MyPlayer = myPlayer;
                koHandler.MyPlayerObject = myPlayerObject;
                koHandler.MyAudioRecorder = myAudioRecorder;
                koHandler.MyAudioRecorder.VoiceDetection = true;

                koHandler.RagdollItemBehaviour = configRagdollItemBehaviour;
                koHandler.RagdollBlurMode = configRagdollBlurMode;
                koHandler.RagdollMuteMic = configRagdollMuteMic;
                koHandler.RagdollTriggerByWorld = configRagdollTriggerByWorld;
                koHandler.RagdollTriggerByItems = configRagdollTriggerByItems;
                koHandler.RagdollTriggerForceWorld = configRagdollTriggerForceWorld;
                koHandler.RagdollTriggerForceItems = configRagdollTriggerForceItems;
                koHandler.RagdollMinimumDuration = configRagdollMinimumDuration;
                koHandler.RagdollMaximumDuration = configRagdollMaximumDuration;

                attached = true;
            }

            else
            {
                Logger.LogInfo("Player is null");
            }
        }

        private void PatchBlurRenderer()
        {
            try
            {
                MoveComponent(myPostSleepObject.GetComponent<Post_Sleep>(), myPostPlayerObject);

                if(configRagdollBlurMode.Value == 2)
                {
                    MoveComponent(myPostSleepObject.GetComponent<Volume>(), myPostPlayerObject);
                }

                myPostPlayerObject.GetComponent<Volume>().weight = 1.0f;

                myPostSleepObject.SetActive(false);
                myPostPlayerObject.SetActive(true);
            }
            catch 
            {
                Logger.LogInfo("Post_Player or Post_Sleep is null");
            }
        }

        private Component MoveComponent(Component original, GameObject destination)
        {
            System.Type type = original.GetType();
            Component copy = destination.GetComponent(type);

            if (copy == null)
            {
                copy = destination.AddComponent(type);
            }

            // Copied fields can be restricted with BindingFlags
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy;
        }

        private void ScanComponentsInScene()
        {
            GameObject[] objects = GameObject.FindObjectsOfType<GameObject>(true);

            try
            {
                foreach (GameObject obj in objects)
                {
                    if (obj.name == "Player(Clone)")
                    {
                        PhotonView actualPhotonView = obj.GetComponent<PhotonView>();

                        if (actualPhotonView.IsMine)
                        {
                            myPlayerObject = obj;
                        }
                    }

                    if (obj.name == "GAME")
                    {
                        GameObject rendering = obj.transform.Find("Rendering").gameObject;
                        myPostPlayerObject = rendering.transform.Find("Post_Player").gameObject;
                        myPostSleepObject = rendering.transform.Find("Post_Sleep").gameObject;
                    }
                }
            }
            catch
            {
                Logger.LogInfo("Player not initialized");
            }
        }
    }

    public class KnockOutHandler : MonoBehaviour
    {
        private ConfigEntry<bool> ragdollMuteMic;
        private ConfigEntry<bool> ragdollTriggerByWorld;
        private ConfigEntry<bool> ragdollTriggerByItems;

        private ConfigEntry<int> ragdollItemBehaviour;
        private ConfigEntry<int> ragdollBlurMode;

        private ConfigEntry<float> ragdollTriggerForceWorld;
        private ConfigEntry<float> ragdollTriggerForceItems;
        private ConfigEntry<float> ragdollMinimumDuration;
        private ConfigEntry<float> ragdollMaximumDuration;

        private Player myPlayer;
        private GameObject myPlayerObject;
        private Recorder myAudioRecorder;

        private float ragdollCooldown;
        private static float offset = 1f;
        private bool knocked = false;

        public Player MyPlayer { get => myPlayer; set => myPlayer = value; }
        public GameObject MyPlayerObject { get => myPlayerObject; set => myPlayerObject = value; }
        public Recorder MyAudioRecorder { get => myAudioRecorder; set => myAudioRecorder = value; }

        
        public ConfigEntry<bool> RagdollMuteMic { get => ragdollMuteMic; set => ragdollMuteMic = value; }
        public ConfigEntry<bool> RagdollTriggerByWorld { get => ragdollTriggerByWorld; set => ragdollTriggerByWorld = value; }
        public ConfigEntry<bool> RagdollTriggerByItems { get => ragdollTriggerByItems; set => ragdollTriggerByItems = value; }
        public ConfigEntry<int> RagdollItemBehaviour { get => ragdollItemBehaviour; set => ragdollItemBehaviour = value; }
        public ConfigEntry<int> RagdollBlurMode { get => ragdollBlurMode; set => ragdollBlurMode = value; }
        public ConfigEntry<float> RagdollTriggerForceWorld { get => ragdollTriggerForceWorld; set => ragdollTriggerForceWorld = value; }
        public ConfigEntry<float> RagdollTriggerForceItems { get => ragdollTriggerForceItems; set => ragdollTriggerForceItems = value; }
        public ConfigEntry<float> RagdollMinimumDuration { get => ragdollMinimumDuration; set => ragdollMinimumDuration = value; }
        public ConfigEntry<float> RagdollMaximumDuration { get => ragdollMaximumDuration; set => ragdollMaximumDuration = value; }


        void OnCollisionEnter(Collision collider)
        {

            // If an item hit the head's hitbox
            if (collider.gameObject.GetComponent<ItemInstance>() != null && ragdollTriggerByItems.Value && !knocked)
            {
                if (collider.impulse.magnitude > ragdollTriggerForceItems.Value)
                {
                    Ragdoll(collider.impulse.magnitude);
                }
            }

            // If anything else hit the head's hitbox
            else if (ragdollTriggerByWorld.Value && !knocked)
            {
                if (collider.impulse.magnitude > ragdollTriggerForceWorld.Value)
                {
                    Ragdoll(collider.impulse.magnitude);
                }
            }
        }

        private void Ragdoll(float magnitude)
        {
            knocked = true;
            ragdollCooldown = (magnitude / 150);

            if (ragdollBlurMode.Value != 0)
            {
                myPlayer.data.sleepAmount = ragdollCooldown + (offset * 1.5f);
            }

            if (ragdollMuteMic.Value)
            {
                myAudioRecorder.RecordingEnabled = false;
            }

            if (ragdollCooldown < RagdollMinimumDuration.Value)
            {
                ragdollCooldown = RagdollMinimumDuration.Value;
            }

            if (ragdollCooldown > RagdollMaximumDuration.Value)
            {
                ragdollCooldown = RagdollMaximumDuration.Value;
            }

            myPlayer.data.inputOverideAmount = 1;

            Fall(ragdollCooldown);

        }

        public void Update()
        {

            if (myPlayer.data.fallTime <= 0.5f && knocked)
            {
                if(RagdollItemBehaviour.Value == 2)
                {
                    myPlayer.RPC_SelectSlot(-1);
                }
                else if(RagdollItemBehaviour.Value == 1)
                {
                    myPlayer.data.dropItemsFor = 0.1f;
                }
                myPlayer.data.fallTime = 0f;
            }

            if (myPlayer.data.fallTime <= 0f && knocked)
            {
                Recover();
            }
        }

        private void Fall(float duration)
        {
            myPlayer.refs.ragdoll.StartCoroutine("CallFall", duration);
            myPlayer.data.fallTime = duration;
        }

        private void Recover()
        {
            myPlayer.data.inputOverideAmount = 0;
            myAudioRecorder.RecordingEnabled = true;
            knocked = false;
        }
    }
}
