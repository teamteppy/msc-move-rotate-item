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
        public override string Version => "1.0";
        public override string Description => "More rotation and movement with item pickup: Hold down Shift or Alt or Tab";
        public override Game SupportedGames => Game.MySummerCar;

        private FsmGameObject pickedObject;
        private PlayMakerFSM pickUpFsm;
        private Camera fpsCamera;
        private Transform itemPivot;

        private GameObject hijackedGO;
        private bool shiftHijacked = false;

        private GameObject altHijackedGO;
        private bool altHijacked = false;

        private GameObject middleHijackedGO;
        private bool middleHijacked = false;

        private GameObject tabHijackedGO;
        private bool tabHijacked = false;
        private Vector3 tabHijackOrigin;

        private const float TAB_MOUSE_SENSITIVITY = 0.05f;
        private const float TAB_CLAMP_RADIUS = 0.5f;

        private FsmBool handEmpty;
        private FsmGameObject raycastHitObject;
        private FsmInt lenght;

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.OnGUI, Mod_OnGUI);
            SetupFunction(Setup.Update, Mod_Update);
            SetupFunction(Setup.ModSettings, Mod_Settings);
        }

        private void Mod_Settings()
        {
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

        private void Mod_OnGUI()
        {

        }

        private void Mod_Update()
        {
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool middleHeld = Input.GetMouseButton(2);
            bool tabHeld = Input.GetKey(KeyCode.Tab);
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
                ReleaseShiftHijack();
            }

            if (altHeld && !altHijacked && pickedObject != null && pickedObject.Value != null)
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
            else if (!altHeld && altHijacked)
            {
                ReleaseAltHijack();
            }

            if (middleHeld && !middleHijacked && pickedObject != null && pickedObject.Value != null)
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
            else if (!middleHeld && middleHijacked)
            {
                ReleaseMiddleHijack();
            }

            if (tabHeld && !tabHijacked && pickedObject != null && pickedObject.Value != null)
            {
                tabHijackedGO = pickedObject.Value;

                tabHijackOrigin = new Vector3(tabHijackedGO.transform.position.x, 0f, tabHijackedGO.transform.position.z);

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

                    Vector3 newPivotPosition = itemPivot.position + moveRight + moveForward;

                    Vector3 originFlat = new Vector3(tabHijackOrigin.x, newPivotPosition.y, tabHijackOrigin.z);
                    Vector3 offsetFromOrigin = newPivotPosition - originFlat;

                    if (offsetFromOrigin.magnitude > TAB_CLAMP_RADIUS)
                    {
                        offsetFromOrigin = offsetFromOrigin.normalized * TAB_CLAMP_RADIUS;
                        newPivotPosition = originFlat + offsetFromOrigin;
                    }

                    Vector3 moveDelta = newPivotPosition - itemPivot.position;

                    itemPivot.position = newPivotPosition;
                    tabHijackedGO.transform.position += moveDelta;
                }
            }

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

            middleHijackedGO = null;
            middleHijacked = false;
        }

        private void ReleaseTabHijack()
        {
            if (tabHijackedGO == null) { return; }

            tabHijackedGO.transform.SetParent(itemPivot, true);

            pickedObject.Value = tabHijackedGO;
            raycastHitObject.Value = tabHijackedGO;
            handEmpty.Value = false;
            lenght.Value = tabHijackedGO.name.Length;

            Rigidbody rb = tabHijackedGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            pickUpFsm.SendEvent("FINISHED");

            tabHijackedGO = null;
            tabHijacked = false;
        }
    }
}