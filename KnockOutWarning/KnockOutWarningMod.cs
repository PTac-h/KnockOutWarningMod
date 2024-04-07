using MelonLoader;
using KnockOutWarning;
using UnityEngine;
using Photon.Pun;
using Steamworks;
using System.Collections.Generic;
using UnityEngine.Rendering;

[assembly: MelonInfo(typeof(KnockOutWarningMod), "Knock Out Warning !", "1.0", "PTA-C")]
[assembly: MelonGame("Landfall Games", "Content Warning")]

namespace KnockOutWarning
{
    public class KnockOutHandler : MonoBehaviour
    {

        private string mySteamId;
        private Player myPlayer;
        private PlayerRagdoll myPlayerRagdoll;
        private GameObject myVoiceObject;
        private GameObject myPostPlayer;
        private Volume myBlur;

        public string MySteamId { get => mySteamId; set => mySteamId = value; }
        public Player MyPlayer { get => myPlayer; set => myPlayer = value; }
        public GameObject MyVoiceObject { get => myVoiceObject; set => myVoiceObject = value; }
        public Volume MyBlur { get => myBlur; set => myBlur = value; }
        public GameObject MyPostPlayer { get => myPostPlayer; set => myPostPlayer = value; }
        public PlayerRagdoll MyPlayerRagdoll { get => myPlayerRagdoll; set => myPlayerRagdoll = value; }

        void OnCollisionEnter(Collision collider)
        {

            // If an item hit the head's hitbox
            if (collider.gameObject.GetComponent<ItemInstance>() != null)
            {
                if (collider.impulse.magnitude > 25)
                {
                    Ragdoll(collider.impulse.magnitude);
                }
            }

            // If anything else hit the head's hitbox
            else
            {
                if (collider.impulse.magnitude > 250)
                {
                    Ragdoll(collider.impulse.magnitude);
                }
            }
        }

        private void Ragdoll(float magnitude)
        {
            AddEffects();

            float cooldown = (magnitude / 150);

            if (cooldown < 1.0f)
            {
                cooldown = 1.0f;
            }

            myPostPlayer.SetActive(true);
            myBlur.weight = cooldown;

            myPlayer.refs.ragdoll.StartCoroutine("CallFall", cooldown);

            Invoke("RemoveEffects", cooldown);
        }



        public void Update()
        {
            if (myBlur.weight > 0)
            {
                myBlur.weight -= 0.001f;
            }
            else
            {
                myBlur.weight = 0f;
            }
        }

        private void AddEffects()
        {
            Photon.Voice.Unity.Recorder voiceRecorder = myVoiceObject.GetComponent<Photon.Voice.Unity.Recorder>();
            voiceRecorder.TransmitEnabled = false;
            myPostPlayer.SetActive(true);
        }

        private void RemoveEffects()
        {
            Photon.Voice.Unity.Recorder voiceRecorder = myVoiceObject.GetComponent<Photon.Voice.Unity.Recorder>();
            voiceRecorder.TransmitEnabled = true;
            myPostPlayer.SetActive(false);
        }
    }
    public class KnockOutWarningMod : MelonMod
    {
        private string mySteamId = "";
        private GameObject myPlayerObject = null;
        private GameObject myVoiceObject = null;
        private GameObject myPostPlayer = null;
        private Volume myBlur = null;
        private bool attached;

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 0)
            {
                mySteamId = SteamUser.GetSteamID().ToString();
                attached = true;
            }
            else
            {
                attached = false;
            }
        }
        public override void OnUpdate()
        {
            if(myPlayerObject == null)
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

                attached = true;
            }

            else
            {
                MelonLogger.Msg("Player is null");
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
                        Photon.Realtime.Player playerinfos = actualPhotonView.Owner;
                        ExitGames.Client.Photon.Hashtable table = playerinfos.CustomProperties;

                        string extractedID = table["SteamID"].ToString();

                        if (extractedID == mySteamId.ToString())
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
                MelonLogger.Msg("Something went wrong during player scan");
            }
        }
    }
}
