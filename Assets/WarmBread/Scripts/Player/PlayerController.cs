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
        public string Prompt { get; private set; }

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
        private Transform hands;
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

            sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, sensitivity), .025f, .25f);
            headBob = PlayerPrefs.GetInt(HeadBobKey, 1) != 0;

            var cameraObject = new GameObject("Камера");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = Vector3.up * 1.62f;

            View = cameraObject.AddComponent<Camera>();
            View.fieldOfView = 68f;
            View.nearClipPlane = .035f;
            View.farClipPlane = 140f;
            View.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();

            hands = new GameObject("Руки и бумажный пакет").transform;
            hands.SetParent(View.transform, false);

            var cloth = WorldArt.Material("Свитер", new Color(.38f, .28f, .2f));
            var skin = WorldArt.Material("Кисти рук", new Color(.73f, .54f, .4f));
            for (var side = -1; side <= 1; side += 2)
            {
                var sleeve = WorldArt.Part(
                    "Sleeve",
                    hands,
                    new Vector3(side * .27f, -.38f, .31f),
                    new Vector3(.12f, .13f, .34f),
                    cloth);
                sleeve.transform.localRotation = Quaternion.Euler(-16f, -side * 18f, 0f);

                WorldArt.Part(
                    "Hand",
                    hands,
                    new Vector3(side * .22f, -.3f, .49f),
                    new Vector3(.095f, .07f, .15f),
                    skin);
            }

            held = WorldArt.Part(
                "Пакет",
                hands,
                new Vector3(.12f, -.29f, .57f),
                new Vector3(.21f, .25f, .19f),
                WorldArt.Material("Бумага", new Color(.7f, .55f, .36f)));
        }

        private void Update()
        {
            if (session == null || controller == null || View == null) return;

            var active = session.Running && !session.Paused && !session.Modal;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;

            if (session.Bag.Count != bagCount)
            {
                bagCount = session.Bag.Count;
                if (held != null) held.SetActive(bagCount > 0);
            }

            if (!active || Keyboard.current == null || Mouse.current == null)
            {
                Prompt = string.Empty;
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

            gravity = controller.isGrounded ? -2f : gravity - 18f * Time.deltaTime;
            var velocity = (transform.right * move.x + transform.forward * move.y) * 1.85f +
                           Vector3.up * gravity;
            controller.Move(velocity * Time.deltaTime);

            step += Time.deltaTime * move.magnitude * 8f;
            View.transform.localPosition = new Vector3(
                0f,
                1.62f + (headBob ? Mathf.Sin(step) * .012f * move.magnitude : 0f),
                0f);

            if (hands != null)
            {
                hands.localPosition = new Vector3(
                    Mathf.Sin(Time.time * 1.1f) * .004f,
                    Mathf.Sin(Time.time * 1.7f) * .004f,
                    0f);
            }

            focus = Mathf.Lerp(focus, mouse.rightButton.isPressed ? 1f : 0f, Time.deltaTime * 9f);
            View.fieldOfView = Mathf.Lerp(68f, 53f, focus);

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
