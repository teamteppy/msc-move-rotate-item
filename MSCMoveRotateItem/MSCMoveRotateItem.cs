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
        private PlayMakerGlobals globals;
        private SettingsKeybind debugKey;
        private Transform itemPivot;
        private GameObject heldGO;
        private float zRotationOffset = 0f;

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
        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
        private void TryFindItemPivot()
        {
            if (itemPivot != null) { return; }
            GameObject player = GameObject.Find("PLAYER");
            if (player == null) { return; }
            itemPivot = player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/ItemPivot");
        }

        private void Mod_Settings()
        {
            debugKey = Keybind.Add("DebugKey", "Debug Game", KeyCode.Alpha9);
        }
        private void Mod_OnLoad()
        {
            globals = PlayMakerGlobals.Instance;
        }
        private void Mod_OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperCenter;

            string heldName;
            if (heldGO == null)
            {
                heldName = "NULL";
            }
            else
            {
                heldName = heldGO.name;
            }

            GUI.Label(new Rect(0, 20, Screen.width, 60), $"ItemPivot: {(itemPivot != null ? "FOUND" : "NULL")}  |  HeldGO: {heldName}", style);
        }
        private void Mod_Update()
        {
            TryFindItemPivot();

            heldGO = null;
            if (itemPivot != null && itemPivot.childCount > 0)
            {
                heldGO = itemPivot.GetChild(0).gameObject;
            }

            if (debugKey.GetKeybindDown())
            {
                //string[] path = new string[] { "PLAYER", "Pivot", "AnimPivot", "Camera", "FPSCamera", "1Hand_Assemble", "ItemPivot" };
                //GameObject root = GameObject.Find("PLAYER");
                //Transform current = root.transform;
                //foreach (string step in path)
                //{
                //    if (step == "PLAYER") { continue; }
                //    current = current.Find(step);
                //    if (current == null)
                //    {
                //        LogToFile($"PATH BROKE at: {step}");
                //        break;
                //    }
                //    LogToFile($"Found: {current.name} childCount={current.childCount}");
                //}
                //if (current != null)
                //{
                //    LogToFile($"Final node children:");
                //    foreach (Transform child in current)
                //    {
                //        LogToFile($"  child: {child.name} active={child.gameObject.activeSelf}");
                //    }
                //}

                GameObject player = GameObject.Find("PLAYER");
                if (player != null)
                {
                    LogToFile($"Dumping FSMs on PLAYER:");
                    foreach (PlayMakerFSM fsm in player.GetComponents<PlayMakerFSM>())
                    {
                        LogToFile($"  FSM: '{fsm.FsmName}'  currentState='{fsm.ActiveStateName}'");
                        foreach (var state in fsm.FsmStates)
                        {
                            LogToFile($"    state: {state.Name}");
                        }
                    }
                }
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel"); 
            if (scroll != 0f && itemPivot != null)
            {
                LogToFile($"[SCROLL] ItemPivot localRot={itemPivot.localEulerAngles}  heldGO localRot={heldGO?.transform.localEulerAngles}");
            }
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (shiftHeld && scroll != 0f)
            {
                zRotationOffset += scroll * 80f;
            }

            if (heldGO == null) { return; }

            Vector3 angles = heldGO.transform.localEulerAngles;
            angles.z = zRotationOffset;
            heldGO.transform.localEulerAngles = angles;
        }
    }
}