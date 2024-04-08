using BepInEx;
using BepInEx.Configuration;
using UnityEngine.Rendering;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

namespace KnockOutWarning
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private GameObject myPlayerObject = null;
        private GameObject myVoiceObject = null;
        private GameObject myPostPlayer = null;

        private Volume myBlur = null;
        private bool attached;

        private static ConfigEntry<bool> configRagdollBlurVision { get; set; }
        private static ConfigEntry<bool> configRagdollMuteMic { get; set; }
        private static ConfigEntry<bool> configRagdollTriggerByWorld { get; set; }
        private static ConfigEntry<bool> configRagdollTriggerByItems { get; set; }

        private static ConfigEntry<float> configRagdollTriggerForceWorld { get; set; }
        private static ConfigEntry<float> configRagdollTriggerForceItems { get; set; }

        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            configRagdollBlurVision = Config.Bind("General.Toggles", "RagdollBlurVision", true, "Dictates if the vision is blurry when being in a \"blackout\" state.");
            configRagdollMuteMic = Config.Bind("General.Toggles", "RagdollMuteMic", true,"Dictates if the mic is switched off when being in a \"blackout\" state.");
            configRagdollTriggerByWorld = Config.Bind("General.Toggles", "RagdollTriggerByWorld", true,"Dictates if the ragdoll is triggered when being hit by terrain and static objects.");
            configRagdollTriggerByItems = Config.Bind("General.Toggles","RagdollTriggerByItems",true,"Dictates if the ragdoll is triggered when being hit by items and dynamic objects.");
            configRagdollTriggerForceWorld = Config.Bind("General","RagdollTriggerForceWorld",250.0f,"The amount of force to trigger the ragdoll when hit by terrain and static objects.");
            configRagdollTriggerForceItems = Config.Bind("General","RagdollForceTriggerItems",25.0f,"The amount of force to trigger the ragdoll when hit by items and dynamic objetcs.");

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
            if(myPlayerObject == null && !attached) 
            {
                ScanPlayerInScene();
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

                GameObject headPosition = myPlayerObject.transform.Find("HeadPosition").gameObject;
                myVoiceObject = headPosition.transform.Find("Voice").gameObject;

                GameObject rigCreator = myPlayerObject.transform.Find("RigCreator").gameObject;
                GameObject rig = rigCreator.transform.Find("Rig").gameObject;
                GameObject armature = rig.transform.Find("Armature").gameObject;
                GameObject hip = armature.transform.Find("Hip").gameObject;
                GameObject torso = hip.transform.Find("Torso").gameObject;
                GameObject head = torso.transform.Find("Head").gameObject;

                Player player = myPlayerObject.GetComponent<Player>();

                KnockOutHandler koHandler = head.AddComponent<KnockOutHandler>();
                SphereCollider KOCollider = head.AddComponent<SphereCollider>();

                KOCollider.isTrigger = true;
                KOCollider.radius = 1;

                koHandler.MyPlayer = player;
                koHandler.MyVoiceObject = myVoiceObject;
                koHandler.MyBlur = myBlur;
                koHandler.MyPostPlayer = myPostPlayer;
                koHandler.MyPlayerRagdoll = myPlayerObject.GetComponent<PlayerRagdoll>();

                koHandler.RagdollBlurVision = configRagdollBlurVision;
                koHandler.RagdollMuteMic = configRagdollMuteMic;
                koHandler.RagdollTriggerByWorld = configRagdollTriggerByWorld;
                koHandler.RagdollTriggerByItems = configRagdollTriggerByItems;
                koHandler.RagdollTriggerForceWorld = configRagdollTriggerForceWorld;
                koHandler.RagdollTriggerForceItems = configRagdollTriggerForceItems;

                attached = true;
            }

            else
            {
                Logger.LogInfo("Player is null");
            }
        }

        private void ScanPlayerInScene()
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

                            GameObject ingameGameObj = GameObject.Find("GAME").gameObject;
                            GameObject renderingObj = ingameGameObj.transform.Find("Rendering").gameObject;

                            myPostPlayer = renderingObj.transform.Find("Post_Player").gameObject;
                            myBlur = myPostPlayer.GetComponent<Volume>();
                        }
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
        private ConfigEntry<bool> ragdollBlurVision;
        private ConfigEntry<bool> ragdollMuteMic;
                     
        private ConfigEntry<bool> ragdollTriggerByWorld;
        private ConfigEntry<bool> ragdollTriggerByItems;
                     
        private ConfigEntry<float> ragdollTriggerForceWorld;
        private ConfigEntry<float> ragdollTriggerForceItems;

        private Player myPlayer;
        private PlayerRagdoll myPlayerRagdoll;
        private GameObject myVoiceObject;
        private GameObject myPostPlayer;
        private Volume myBlur;

        public Player MyPlayer { get => myPlayer; set => myPlayer = value; }
        public GameObject MyVoiceObject { get => myVoiceObject; set => myVoiceObject = value; }
        public Volume MyBlur { get => myBlur; set => myBlur = value; }
        public GameObject MyPostPlayer { get => myPostPlayer; set => myPostPlayer = value; }
        public PlayerRagdoll MyPlayerRagdoll { get => myPlayerRagdoll; set => myPlayerRagdoll = value; }
        public ConfigEntry<bool> RagdollBlurVision { get => ragdollBlurVision; set => ragdollBlurVision = value; }
        public ConfigEntry<bool> RagdollMuteMic { get => ragdollMuteMic; set => ragdollMuteMic = value; }
        public ConfigEntry<bool> RagdollTriggerByWorld { get => ragdollTriggerByWorld; set => ragdollTriggerByWorld = value; }
        public ConfigEntry<bool> RagdollTriggerByItems { get => ragdollTriggerByItems; set => ragdollTriggerByItems = value; }
        public ConfigEntry<float> RagdollTriggerForceWorld { get => ragdollTriggerForceWorld; set => ragdollTriggerForceWorld = value; }
        public ConfigEntry<float> RagdollTriggerForceItems { get => ragdollTriggerForceItems; set => ragdollTriggerForceItems = value; }

        void OnCollisionEnter(Collision collider)
        {

            // If an item hit the head's hitbox
            if (collider.gameObject.GetComponent<ItemInstance>() != null && ragdollTriggerByItems.Value)
            {
                if (collider.impulse.magnitude > ragdollTriggerForceItems.Value)
                {
                    Ragdoll(collider.impulse.magnitude);
                }
            }

            // If anything else hit the head's hitbox
            else if (ragdollTriggerByWorld.Value)
            {
                if (collider.impulse.magnitude > ragdollTriggerForceWorld.Value)
                {
                    Ragdoll(collider.impulse.magnitude);
                }
            }
        }

        private void Ragdoll(float magnitude)
        {
            AddEffects();

            float cooldown = (magnitude / 150);

            if (cooldown < 1.5f)
            {
                cooldown = 1.5f;
            }

            myPlayer.refs.ragdoll.StartCoroutine("CallFall", cooldown);
            Invoke("RemoveEffects", cooldown);
        }

        public void Update()
        {
            if (myBlur.weight > 0)
            {
                myBlur.weight -= 0.1f;
            }
            else
            {
                myBlur.weight = 0f;
            }
        }

        private void AddEffects()
        {
            if (ragdollMuteMic.Value)
            {
                Photon.Voice.Unity.Recorder voiceRecorder = myVoiceObject.GetComponent<Photon.Voice.Unity.Recorder>();
                voiceRecorder.TransmitEnabled = false;
            }

            if (ragdollBlurVision.Value)
            {
                myBlur.weight = 100;
                myPostPlayer.SetActive(true);
            }
        }

        private void RemoveEffects()
        {
            Photon.Voice.Unity.Recorder voiceRecorder = myVoiceObject.GetComponent<Photon.Voice.Unity.Recorder>();
            voiceRecorder.TransmitEnabled = true;
            myPostPlayer.SetActive(false);
        }
    }
}
