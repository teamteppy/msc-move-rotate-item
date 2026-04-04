using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace MSCMoveRotateItem
{
    public class MSCMoveRotateItem : Mod
    {
        public override string ID => "MSCMoveRotateItem";
        public override string Name => "Move and Rotate Item";
        public override string Author => "teamteppy";
        public override string Version => "1.2";
        public override string Description => "More rotation and movement with item pickup: Hold down Shift or Alt or Tab";
        public override Game SupportedGames => Game.MySummerCar;

        private SettingsKeybind debugKey;
        private FsmGameObject pickedObject;
        private PlayMakerFSM pickUpFsm;
        private Camera fpsCamera;
        private Transform itemPivot;

        private const int RELEASE_FRAME_DELAY = 10;

        private GameObject hijackedGO;
        private bool shiftHijacked = false;
        private int shiftPendingReleaseFrames = 0;
        private float shiftHeldSince = 0f;

        private GameObject altHijackedGO;
        private bool altHijacked = false;
        private int altPendingReleaseFrames = 0;
        private float altHeldSince = 0f;

        private const float MIN_HOLD_DURATION = 0.1f;

        private GameObject middleHijackedGO;
        private bool middleHijacked = false;
        private int middlePendingReleaseFrames = 0;
        private float middleLastReleaseTime = 0f;
        private const float MIDDLE_COOLDOWN = 1.5f;

        private GameObject tabHijackedGO;
        private bool tabHijacked = false;
        private Vector3 tabHijackOrigin;
        private Vector3 tabItemWorldPosition;
        private float tabHeldSince = 0f;
        private int tabHijackedOriginalLayer;

        private const float TAB_MOUSE_SENSITIVITY = 0.05f;
        private const float TAB_CLAMP_RADIUS = 0.5f;

        private FsmBool handEmpty;
        private FsmGameObject raycastHitObject;
        private FsmInt lenght;

        private bool shiftHeldLastFrame = false;
        private bool altHeldLastFrame = false;
        private bool tabHeldLastFrame = false;

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.Update, Mod_Update);
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
                        if (v.Name == "PickedObject") { pickedObject = v; }
                        if (v.Name == "RaycastHitObject") { raycastHitObject = v; }
                    }
                    foreach (var v in fsm.FsmVariables.BoolVariables)
                    {
                        if (v.Name == "HandEmpty") { handEmpty = v; }
                    }
                    foreach (var v in fsm.FsmVariables.IntVariables)
                    {
                        if (v.Name == "Lenght") { lenght = v; }
                    }
                }
            }

            itemPivot = player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/ItemPivot");
        }

        //private void LogToFile(string message)
        //{
        //    string path = Application.persistentDataPath + "/MSCPauseMod_debug.txt";
        //    System.IO.File.AppendAllText(path, message + "\n");
        //}

        private bool IsAllowedItem(GameObject go)
        {
            int[] allowedLayers = new int[]
            {
                16,
            };

            foreach (int layer in allowedLayers)
            {
                if (go.layer == layer) { return true; }
            }
            return false;
        }

        private void Mod_Update()
        {
            //if (debugKey.GetKeybindDown())
            //{
            //    GameObject beerCase = GameObject.Find("beer case(itemx)");
            //    if (beerCase != null)
            //    {
            //        Rigidbody rb = beerCase.GetComponent<Rigidbody>();
            //        string info = "=== beer case(itemx) STATE ===\n"
            //            + "  Layer: " + beerCase.layer + " (" + LayerMask.LayerToName(beerCase.layer) + ")\n"
            //            + "  Tag: " + beerCase.tag + "\n"
            //            + "  Position: " + beerCase.transform.position + "\n"
            //            + "  Active: " + beerCase.activeSelf + "\n"
            //            + "  Parent: " + (beerCase.transform.parent != null ? beerCase.transform.parent.name : "None") + "\n"
            //            + "  Has Rigidbody: " + (rb != null) + "\n"
            //            + "  Is Kinematic: " + (rb != null ? rb.isKinematic.ToString() : "N/A") + "\n"
            //            + "  Velocity: " + (rb != null ? rb.velocity.ToString() : "N/A") + "\n"
            //            + "  shiftHijacked: " + shiftHijacked + "\n"
            //            + "  altHijacked: " + altHijacked + "\n"
            //            + "  middleHijacked: " + middleHijacked + "\n"
            //            + "  tabHijacked: " + tabHijacked + "\n"
            //            + "  shiftPendingReleaseFrames: " + shiftPendingReleaseFrames + "\n"
            //            + "  altPendingReleaseFrames: " + altPendingReleaseFrames + "\n"
            //            + "  FSM state: " + pickUpFsm.ActiveStateName + "\n"
            //            + "  pickedObject: " + (pickedObject.Value != null ? pickedObject.Value.name : "NULL");
            //        LogToFile(info);
            //    }
            //    else
            //    {
            //        LogToFile("beer case(itemx) not found in scene.");
            //    }
            //}

            bool inToolMode = pickUpFsm.gameObject.name != "Hand";

            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool tabHeld = Input.GetKey(KeyCode.Tab);
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (!shiftHeldLastFrame && shiftHeld)
            {
                shiftHeldSince = Time.time;
            }

            if (!altHeldLastFrame && altHeld)
            {
                altHeldSince = Time.time;
            }

            if (!tabHeldLastFrame && tabHeld)
            {
                tabHeldSince = Time.time;
            }

            // shift
            if (shiftPendingReleaseFrames > 0)
            {
                shiftPendingReleaseFrames--;
                if (shiftPendingReleaseFrames == 0)
                {
                    ReleaseShiftHijack();
                }
            }
            else if (!inToolMode && shiftHeld && !shiftHijacked && shiftPendingReleaseFrames == 0 && Time.time - shiftHeldSince > MIN_HOLD_DURATION && pickedObject != null && pickedObject.Value != null && IsAllowedItem(pickedObject.Value))
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

            if (shiftHijacked && !shiftHeld && shiftPendingReleaseFrames == 0)
            {
                shiftPendingReleaseFrames = RELEASE_FRAME_DELAY;
                shiftHeldSince = Time.time;
            }

            // alt
            if (altPendingReleaseFrames > 0)
            {
                altPendingReleaseFrames--;
                if (altPendingReleaseFrames == 0)
                {
                    ReleaseAltHijack();
                }
            }
            else if (!inToolMode && altHeld && !altHijacked && altPendingReleaseFrames == 0 && Time.time - altHeldSince > MIN_HOLD_DURATION && pickedObject != null && pickedObject.Value != null && IsAllowedItem(pickedObject.Value))
            {
                altHijackedGO = pickedObject.Value;

                Rigidbody rb = altHijackedGO.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                altHijackedGO.transform.SetParent(fpsCamera.transform, true);
                pickedObject.Value = null;
                pickUpFsm.SendEvent("DROP_PART");
                altHijacked = true;
            }

            if (altHijacked && !altHeld && altPendingReleaseFrames == 0)
            {
                altPendingReleaseFrames = RELEASE_FRAME_DELAY;
                altHeldSince = Time.time;
            }

            // middle
            if (middlePendingReleaseFrames > 0)
            {
                middlePendingReleaseFrames--;
                if (middlePendingReleaseFrames == 0)
                {
                    ReleaseMiddleHijack();
                }
            }
            else if (!inToolMode && Input.GetMouseButtonDown(2) && middlePendingReleaseFrames == 0 && !middleHijacked && Time.time - middleLastReleaseTime > MIDDLE_COOLDOWN && pickedObject != null && pickedObject.Value != null && IsAllowedItem(pickedObject.Value))
            {
                middleHijackedGO = pickedObject.Value;

                Rigidbody rb = middleHijackedGO.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                middleHijackedGO.transform.SetParent(fpsCamera.transform, true);
                pickedObject.Value = null;
                pickUpFsm.SendEvent("DROP_PART");
                middleHijacked = true;
            }

            if (middleHijacked && !Input.GetMouseButton(2) && middlePendingReleaseFrames == 0)
            {
                middlePendingReleaseFrames = RELEASE_FRAME_DELAY;
            }

            // tab
            if (!inToolMode && tabHeld && Time.time - tabHeldSince > MIN_HOLD_DURATION && !tabHijacked && pickedObject != null && pickedObject.Value != null && IsAllowedItem(pickedObject.Value))
            {
                tabHijackedGO = pickedObject.Value;
                tabHijackedOriginalLayer = tabHijackedGO.layer;

                tabHijackOrigin = new Vector3(tabHijackedGO.transform.position.x, 0f, tabHijackedGO.transform.position.z);
                tabItemWorldPosition = new Vector3(tabHijackedGO.transform.position.x, tabHijackedGO.transform.position.y, tabHijackedGO.transform.position.z);

                Rigidbody rb = tabHijackedGO.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                tabHijackedGO.transform.SetParent(fpsCamera.transform, true);
                pickedObject.Value = null;
                pickUpFsm.SendEvent("DROP_PART");
                tabHijacked = true;
            }
            else if (!tabHeld && tabHijacked)
            {
                ReleaseTabHijack();
            }

            if (shiftHijacked && scroll != 0f)
            {
                if (hijackedGO != null)
                {
                    hijackedGO.transform.Rotate(0f, 0f, scroll * 80f, Space.Self);
                }
            }

            if (altHijacked && scroll != 0f)
            {
                if (altHijackedGO != null)
                {
                    altHijackedGO.transform.Rotate(0f, scroll * 80f, 0f, Space.Self);
                }
            }

            if (middleHijacked)
            {
                if (middleHijackedGO != null)
                {
                    middleHijackedGO.transform.localRotation = Quaternion.identity;
                }
            }

            if (tabHijacked)
            {
                if (tabHijackedGO != null)
                {
                    float mouseDeltaX = Input.GetAxis("Mouse X");
                    float mouseDeltaY = Input.GetAxis("Mouse Y");

                    Vector3 cameraForwardFlat = fpsCamera.transform.forward;
                    cameraForwardFlat.y = 0f;
                    cameraForwardFlat.Normalize();

                    Vector3 cameraRightFlat = fpsCamera.transform.right;
                    cameraRightFlat.y = 0f;
                    cameraRightFlat.Normalize();

                    Vector3 moveRight = cameraRightFlat * mouseDeltaX * TAB_MOUSE_SENSITIVITY;
                    Vector3 moveForward = cameraForwardFlat * mouseDeltaY * TAB_MOUSE_SENSITIVITY;

                    tabItemWorldPosition.x += moveRight.x + moveForward.x;
                    tabItemWorldPosition.z += moveRight.z + moveForward.z;

                    Vector3 offsetFromOrigin = new Vector3(tabItemWorldPosition.x - tabHijackOrigin.x, 0f, tabItemWorldPosition.z - tabHijackOrigin.z);

                    if (offsetFromOrigin.magnitude > TAB_CLAMP_RADIUS)
                    {
                        offsetFromOrigin = offsetFromOrigin.normalized * TAB_CLAMP_RADIUS;
                        tabItemWorldPosition.x = tabHijackOrigin.x + offsetFromOrigin.x;
                        tabItemWorldPosition.z = tabHijackOrigin.z + offsetFromOrigin.z;
                    }

                    itemPivot.position = tabItemWorldPosition;
                    tabHijackedGO.transform.position = tabItemWorldPosition;
                }
            }

            shiftHeldLastFrame = shiftHeld;
            altHeldLastFrame = altHeld;
            tabHeldLastFrame = tabHeld;
        }

        private void ReleaseShiftHijack()
        {
            if (hijackedGO == null) { return; }

            hijackedGO.transform.SetParent(itemPivot, true);
            hijackedGO.transform.localPosition = Vector3.zero;

            pickedObject.Value = hijackedGO;
            raycastHitObject.Value = hijackedGO;
            handEmpty.Value = false;
            lenght.Value = hijackedGO.name.Length;

            Rigidbody rb = hijackedGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            pickUpFsm.SendEvent("FINISHED");

            hijackedGO = null;
            shiftHijacked = false;
        }

        private void ReleaseAltHijack()
        {
            if (altHijackedGO == null) { return; }

            altHijackedGO.transform.SetParent(itemPivot, true);
            altHijackedGO.transform.localPosition = Vector3.zero;

            pickedObject.Value = altHijackedGO;
            raycastHitObject.Value = altHijackedGO;
            handEmpty.Value = false;
            lenght.Value = altHijackedGO.name.Length;

            Rigidbody rb = altHijackedGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            pickUpFsm.SendEvent("FINISHED");

            altHijackedGO = null;
            altHijacked = false;
        }

        private void ReleaseMiddleHijack()
        {
            if (middleHijackedGO == null) { return; }

            middleHijackedGO.transform.SetParent(itemPivot, true);
            middleHijackedGO.transform.localPosition = Vector3.zero;

            pickedObject.Value = middleHijackedGO;
            raycastHitObject.Value = middleHijackedGO;
            handEmpty.Value = false;
            lenght.Value = middleHijackedGO.name.Length;

            Rigidbody rb = middleHijackedGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            pickUpFsm.SendEvent("FINISHED");

            middleLastReleaseTime = Time.time;
            middleHijackedGO = null;
            middleHijacked = false;
        }

        private void ReleaseTabHijack()
        {
            if (tabHijackedGO == null) { return; }

            tabHijackedGO.transform.SetParent(null);
            tabHijackedGO.layer = LayerMask.NameToLayer("Parts");
            tabHijackedGO.transform.position = tabItemWorldPosition;

            Rigidbody rb = tabHijackedGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.WakeUp();
            }

            pickedObject.Value = null;
            pickUpFsm.SendEvent("DROP_PART");

            tabHijackedGO = null;
            tabHijacked = false;
        }
    }
}