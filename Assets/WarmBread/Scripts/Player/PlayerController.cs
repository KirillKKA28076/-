using UnityEngine;
using UnityEngine.InputSystem;

namespace WarmBread
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const string SensitivityKey = "WarmBread.Player.Sensitivity";
        private const string HeadBobKey = "WarmBread.Player.HeadBob";

        public Camera View { get; private set; }
        public string Prompt { get; private set; } = string.Empty;

        public float Sensitivity
        {
            get => sensitivity;
            set
            {
                sensitivity = Mathf.Clamp(value, .025f, .25f);
                PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
            }
        }

        public bool HeadBob
        {
            get => headBob;
            set
            {
                headBob = value;
                PlayerPrefs.SetInt(HeadBobKey, headBob ? 1 : 0);
            }
        }

        private GameSession session;
        private CharacterController controller;
        private float sensitivity = .085f;
        private bool headBob = true;
        private float pitch;
        private float gravity;
        private float step;
        private float focus;
        private float currentSpeed;
        private Transform hands;
        private Transform leftHand;
        private Transform rightHand;
        private GameObject held;
        private int bagCount = -1;

        public void Initialize(GameSession game)
        {
            session = game;
            controller = GetComponent<CharacterController>();
            controller.height = 1.7f;
            controller.radius = .22f;
            controller.center = Vector3.up * .85f;
            controller.stepOffset = .18f;
            controller.slopeLimit = 48f;

            sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, sensitivity), .025f, .25f);
            headBob = PlayerPrefs.GetInt(HeadBobKey, 1) != 0;

            var cameraObject = new GameObject("Камера");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = Vector3.up * 1.62f;

            View = cameraObject.AddComponent<Camera>();
            View.fieldOfView = 68f;
            View.nearClipPlane = .035f;
            View.farClipPlane = 160f;
            View.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();

            BuildHands();
        }

        private void BuildHands()
        {
            hands = new GameObject("Руки продавца и бумажный пакет").transform;
            hands.SetParent(View.transform, false);

            var cloth = WorldArt.Material("Свитер продавца", new Color(.34f, .24f, .17f), .18f, true);
            var cuff = WorldArt.Material("Манжеты продавца", new Color(.25f, .18f, .13f), .16f, true);
            var skin = WorldArt.Material("Кисти продавца", new Color(.72f, .53f, .4f), .28f);
            var nail = WorldArt.Material("Ногти продавца", new Color(.77f, .62f, .52f), .35f);

            leftHand = BuildHand(-1, cloth, cuff, skin, nail);
            rightHand = BuildHand(1, cloth, cuff, skin, nail);

            var watchMetal = WorldArt.MetalMaterial("Часы продавца", new Color(.25f, .27f, .25f), .6f);
            var watchFace = WorldArt.Material("Циферблат", new Color(.08f, .11f, .1f), .55f, false, .18f);
            WorldArt.ChamferedBox(
                "Ремешок часов",
                leftHand,
                new Vector3(0f, .03f, -.015f),
                new Vector3(.13f, .045f, .14f),
                .018f,
                watchMetal);
            WorldArt.ChamferedBox(
                "Циферблат",
                leftHand,
                new Vector3(0f, .055f, -.085f),
                new Vector3(.085f, .055f, .018f),
                .018f,
                watchFace);

            held = BuildPaperBag();
            held.SetActive(false);
        }

        private Transform BuildHand(
            int side,
            Material cloth,
            Material cuff,
            Material skin,
            Material nail)
        {
            var root = new GameObject(side < 0 ? "Левая рука" : "Правая рука").transform;
            root.SetParent(hands, false);
            root.localPosition = new Vector3(side * .25f, -.34f, .39f);
            root.localRotation = Quaternion.Euler(-16f, -side * 18f, side * 2f);

            WorldArt.ChamferedBox(
                "Рукав",
                root,
                new Vector3(side * .035f, -.02f, -.12f),
                new Vector3(.14f, .14f, .36f),
                .045f,
                cloth);
            WorldArt.ChamferedBox(
                "Манжета",
                root,
                new Vector3(0f, -.01f, .075f),
                new Vector3(.145f, .09f, .1f),
                .025f,
                cuff);
            WorldArt.ChamferedBox(
                "Ладонь",
                root,
                new Vector3(0f, 0f, .18f),
                new Vector3(.13f, .075f, .18f),
                .04f,
                skin);

            for (var finger = 0; finger < 4; finger++)
            {
                var x = (finger - 1.5f) * .027f;
                var length = .11f - Mathf.Abs(finger - 1.5f) * .01f;
                WorldArt.ChamferedBox(
                    "Палец",
                    root,
                    new Vector3(x, -.012f, .29f),
                    new Vector3(.023f, .035f, length),
                    .011f,
                    skin);
                WorldArt.ChamferedBox(
                    "Ноготь",
                    root,
                    new Vector3(x, .008f, .29f + length * .45f),
                    new Vector3(.017f, .008f, .028f),
                    .006f,
                    nail);
            }

            var thumb = WorldArt.ChamferedBox(
                "Большой палец",
                root,
                new Vector3(-side * .072f, -.018f, .22f),
                new Vector3(.035f, .045f, .11f),
                .014f,
                skin);
            thumb.transform.localRotation = Quaternion.Euler(0f, side * 28f, side * -12f);
            return root;
        }

        private GameObject BuildPaperBag()
        {
            var root = new GameObject("Бумажный пакет");
            root.transform.SetParent(hands, false);
            root.transform.localPosition = new Vector3(.09f, -.29f, .62f);
            root.transform.localRotation = Quaternion.Euler(-4f, 7f, 0f);

            var paperBag = WorldArt.Material("Крафтовая бумага", new Color(.64f, .47f, .28f), .12f, true);
            var seam = WorldArt.Material("Складки пакета", new Color(.45f, .31f, .18f), .1f, true);
            WorldArt.ChamferedBox(
                "Пакет",
                root.transform,
                Vector3.zero,
                new Vector3(.34f, .39f, .25f),
                .035f,
                paperBag);
            WorldArt.ChamferedBox(
                "Передняя складка",
                root.transform,
                new Vector3(0f, 0f, -.13f),
                new Vector3(.035f, .31f, .012f),
                .006f,
                seam);
            WorldArt.ChamferedBox(
                "Нижний шов",
                root.transform,
                new Vector3(0f, -.17f, -.13f),
                new Vector3(.28f, .025f, .012f),
                .006f,
                seam);

            var handleMesh = WorldArt.SharedMesh(
                "model:paper-bag-handle",
                () => ProceduralMeshFactory.CreateTorus("Ручка пакета", .09f, .009f, 20, 6));
            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.MeshObject(
                    "Верёвочная ручка",
                    handleMesh,
                    seam,
                    root.transform,
                    new Vector3(side * .095f, .23f, 0f),
                    Quaternion.Euler(90f, 0f, 0f),
                    new Vector3(.72f, 1f, .72f));
            }

            return root;
        }

        private void Update()
        {
            if (session == null || controller == null || View == null) return;

            var active = session.Running && !session.Paused && !session.Modal;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;

            var count = session.Bag.Where(batch => batch != null).Sum(batch => batch.quantity);
            if (count != bagCount)
            {
                bagCount = count;
                if (held != null) held.SetActive(bagCount > 0);
            }

            if (!active || Keyboard.current == null || Mouse.current == null)
            {
                Prompt = string.Empty;
                AnimateHands(Vector2.zero, false);
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var look = mouse.delta.ReadValue() * sensitivity;

            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -73f, 73f);
            View.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            var move = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            move = Vector2.ClampMagnitude(move, 1f);

            var running = keyboard.leftShiftKey.isPressed && move.y > .1f;
            var targetSpeed = running ? 2.8f : 1.85f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * 4.5f);
            gravity = controller.isGrounded ? -2f : gravity - 18f * Time.deltaTime;
            var velocity =
                (transform.right * move.x + transform.forward * move.y) * currentSpeed +
                Vector3.up * gravity;
            controller.Move(velocity * Time.deltaTime);

            step += Time.deltaTime * move.magnitude * (running ? 11f : 8f);
            var bobAmount = headBob ? (running ? .017f : .012f) * move.magnitude : 0f;
            View.transform.localPosition = new Vector3(
                Mathf.Cos(step * .5f) * bobAmount * .42f,
                1.62f + Mathf.Sin(step) * bobAmount,
                0f);

            focus = Mathf.Lerp(focus, mouse.rightButton.isPressed ? 1f : 0f, Time.deltaTime * 9f);
            View.fieldOfView = Mathf.Lerp(running ? 71f : 68f, 53f, focus);
            AnimateHands(move, running);

            Prompt = string.Empty;
            if (Physics.Raycast(
                    View.transform.position,
                    View.transform.forward,
                    out var hit,
                    2.8f,
                    ~0,
                    QueryTriggerInteraction.Collide))
            {
                var target = hit.collider.GetComponentInParent<InteractionTarget>();
                if (target != null)
                {
                    Prompt = "E  ·  " + target.Label;
                    if (keyboard.eKey.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame)
                    {
                        target.Use();
                    }
                }
            }

            if (keyboard.xKey.wasPressedThisFrame) session.ReturnBag();
            if (keyboard.fKey.wasPressedThisFrame) session.Serve();
            if (keyboard.digit1Key.wasPressedThisFrame) TryAddProduct("bread_white");
            if (keyboard.digit2Key.wasPressedThisFrame) TryAddProduct("pirozhok_meat");
            if (keyboard.digit3Key.wasPressedThisFrame) TryAddProduct("lemonade");
            if (keyboard.digit4Key.wasPressedThisFrame) TryAddProduct("gum");
        }

        private void AnimateHands(Vector2 move, bool running)
        {
            if (hands == null) return;
            var movement = move.magnitude;
            var frequency = running ? 8f : 5.4f;
            var sway = Mathf.Sin(Time.time * frequency) * movement;
            hands.localPosition = Vector3.Lerp(
                hands.localPosition,
                new Vector3(sway * .008f, Mathf.Abs(sway) * -.008f, 0f),
                Time.unscaledDeltaTime * 7f);
            hands.localRotation = Quaternion.Lerp(
                hands.localRotation,
                Quaternion.Euler(0f, 0f, -sway * 1.5f),
                Time.unscaledDeltaTime * 7f);

            if (leftHand != null)
            {
                leftHand.localRotation = Quaternion.Lerp(
                    leftHand.localRotation,
                    Quaternion.Euler(-16f + sway * 2f, 18f, -2f),
                    Time.unscaledDeltaTime * 6f);
            }

            if (rightHand != null)
            {
                rightHand.localRotation = Quaternion.Lerp(
                    rightHand.localRotation,
                    Quaternion.Euler(-16f - sway * 2f, -18f, 2f),
                    Time.unscaledDeltaTime * 6f);
            }
        }

        private void TryAddProduct(string id)
        {
            var product = session.FindProduct(id);
            if (product != null) session.AddToBag(product);
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PlayerPrefs.Save();
        }
    }
}
