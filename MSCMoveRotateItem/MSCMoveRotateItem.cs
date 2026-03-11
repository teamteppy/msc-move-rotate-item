using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace MSCMoveRotateItem
{
    public class MSCMoveRotateItem : Mod
    {
        public override string ID => "MSCMoveRotateItem";
        public override string Name => "Move Forward Rotate Item";
        public override string Author => "teamteppy";
        public override string Version => "1.0";
        public override string Description => "";
        public override Game SupportedGames => Game.MySummerCar;

        private SettingsKeybind debugKey;
        private FsmGameObject pickedObject;
        private PlayMakerFSM pickUpFsm;
        private Camera fpsCamera;
        private Transform itemPivot;

        private GameObject hijackedGO;
        private bool shiftHijacked = false;

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.OnGUI, Mod_OnGUI);
            SetupFunction(Setup.Update, Mod_Update);
            SetupFunction(Setup.ModSettings, Mod_Settings);
        }

        private void LogToFile(string message)
        {
            string path = Application.persistentDataPath + "/MSCPauseMod_debug.txt";
            System.IO.File.AppendAllText(path, message + "\n");
        }

        private void Mod_Settings()
        {
            debugKey = Keybind.Add("DebugKey", "Debug Game", KeyCode.Alpha9);
        }

        private void Mod_OnLoad()
        {
            fpsCamera = GameObject.Find("FPSCamera").GetComponent<Camera>();

            GameObject player = GameObject.Find("PLAYER");
            foreach (PlayMakerFSM fsm in player.GetComponentsInChildren<PlayMakerFSM>())
            {
                if (fsm.FsmName == "PickUp")
                {
                    pickUpFsm = fsm;
                    foreach (var v in fsm.FsmVariables.GameObjectVariables)
                    {
                        if (v.Name == "PickedObject")
                        {
                            pickedObject = v;
                            break;
                        }
                    }
                    break;
                }
            }

            itemPivot = player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/ItemPivot");
        }

        private void Mod_OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperCenter;

            string heldName;
            if (pickedObject == null || pickedObject.Value == null)
            {
                heldName = "NULL";
            }
            else
            {
                heldName = pickedObject.Value.name;
            }

            string hijackStatus;
            if (shiftHijacked)
            {
                hijackStatus = "HIJACKED";
            }
            else
            {
                hijackStatus = "normal";
            }

            GUI.Label(new Rect(0, 20, Screen.width, 60), $"HeldGO: {heldName}  |  Status: {hijackStatus}", style);
        }

        private void Mod_Update()
        {
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (shiftHeld && !shiftHijacked && pickedObject != null && pickedObject.Value != null)
            {
                hijackedGO = pickedObject.Value;

                Rigidbody rb = hijackedGO.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                hijackedGO.transform.SetParent(fpsCamera.transform, true);
                pickedObject.Value = null;
                pickUpFsm.SendEvent("DROP_PART");
                shiftHijacked = true;
            }
            else if (!shiftHeld && shiftHijacked)
            {
                ReleaseHijack();
            }

            if (shiftHijacked && scroll != 0f)
            {
                if (hijackedGO != null)
                {
                    hijackedGO.transform.Rotate(0f, 0f, scroll * 80f, Space.Self);
                }
            }

            if (debugKey.GetKeybindDown())
            {
                HutongGames.PlayMaker.FsmBool handEmpty = null;
                foreach (var v in pickUpFsm.FsmVariables.BoolVariables)
                {
                    if (v.Name == "HandEmpty")
                    {
                        handEmpty = v;
                        break;
                    }
                }
                LogToFile($"[DEBUG] HandEmpty={handEmpty?.Value}");
            }
        }

        private void ReleaseHijack()
        {
            if (hijackedGO == null) { return; }

            hijackedGO.transform.SetParent(itemPivot, true);
            hijackedGO.transform.localPosition = Vector3.zero;

            pickedObject.Value = hijackedGO;
            pickUpFsm.SendEvent("LOOP");

            LogToFile($"ReleaseHijack: FSM state after LOOP = '{pickUpFsm.ActiveStateName}'");

            Rigidbody rb = hijackedGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            hijackedGO = null;
            shiftHijacked = false;
        }
    }
}