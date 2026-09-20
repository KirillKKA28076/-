using UnityEngine;
using UnityEngine.InputSystem;

namespace WarmBread
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public Camera View { get; private set; }
        public string Prompt { get; private set; }
        public float Sensitivity { get; set; } = .085f;
        public bool HeadBob { get; set; } = true;
        private GameSession session;
        private CharacterController controller;
        private float pitch, gravity, step, focus;
        private Transform hands;
        private GameObject held;
        private int bagCount = -1;
        public void Initialize(GameSession game)
        {
            session = game; controller = GetComponent<CharacterController>();
            controller.height = 1.7f; controller.radius = .22f; controller.center = Vector3.up * .85f; controller.stepOffset = .18f;
            var cameraObject = new GameObject("Камера"); cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = Vector3.up * 1.62f;
            View = cameraObject.AddComponent<Camera>(); View.fieldOfView = 68; View.nearClipPlane = .035f; View.farClipPlane = 140; View.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            hands = new GameObject("Руки и бумажный пакет").transform; hands.SetParent(View.transform, false);
            var cloth = WorldArt.Material("Свитер", new Color(.38f, .28f, .2f));
            var skin = WorldArt.Material("Кисти рук", new Color(.73f, .54f, .4f));
            for (int i = -1; i <= 1; i += 2)
            {
                var sleeve = WorldArt.Part("Sleeve", hands, new Vector3(i * .27f, -.38f, .31f), new Vector3(.12f, .13f, .34f), cloth);
                sleeve.transform.localRotation = Quaternion.Euler(-16, -i * 18, 0);
                WorldArt.Part("Hand", hands, new Vector3(i * .22f, -.3f, .49f), new Vector3(.095f, .07f, .15f), skin);
            }
            held = WorldArt.Part("Пакет", hands, new Vector3(.12f, -.29f, .57f), new Vector3(.21f, .25f, .19f), WorldArt.Material("Бумага", new Color(.7f,.55f,.36f)));
        }
        private void Update()
        {
            if (session == null) return;
            bool active = session.Running && !session.Paused && !session.Modal;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !active;
            if (session.Bag.Count != bagCount) { bagCount = session.Bag.Count; held.SetActive(bagCount > 0); }
            if (!active || Keyboard.current == null || Mouse.current == null) { Prompt = ""; return; }
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            var look = mouse.delta.ReadValue() * Sensitivity;
            transform.Rotate(0, look.x, 0); pitch = Mathf.Clamp(pitch - look.y, -73, 73);
            View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            var move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            move = Vector2.ClampMagnitude(move, 1);
            gravity = controller.isGrounded ? -2 : gravity - 18 * Time.deltaTime;
            controller.Move(((transform.right * move.x + transform.forward * move.y) * 1.85f + Vector3.up * gravity) * Time.deltaTime);
            step += Time.deltaTime * move.magnitude * 8;
            View.transform.localPosition = new Vector3(0, 1.62f + (HeadBob ? Mathf.Sin(step) * .012f * move.magnitude : 0), 0);
            hands.localPosition = new Vector3(Mathf.Sin(Time.time * 1.1f) * .004f, Mathf.Sin(Time.time * 1.7f) * .004f, 0);
            focus = Mathf.Lerp(focus, mouse.rightButton.isPressed ? 1 : 0, Time.deltaTime * 9); View.fieldOfView = Mathf.Lerp(68, 53, focus);
            Prompt = "";
            if (Physics.Raycast(View.transform.position, View.transform.forward, out var hit, 2.8f, ~0, QueryTriggerInteraction.Collide))
            {
                var target = hit.collider.GetComponent<InteractionTarget>();
                if (target != null) { Prompt = "E  ·  " + target.Label; if (keyboard.eKey.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame) target.Use(); }
            }
            if (keyboard.xKey.wasPressedThisFrame) session.ReturnBag();
            if (keyboard.fKey.wasPressedThisFrame) session.Serve();
            if (keyboard.digit1Key.wasPressedThisFrame) session.AddToBag(session.Products[0]);
            if (keyboard.digit2Key.wasPressedThisFrame) session.AddToBag(session.Products[2]);
            if (keyboard.digit3Key.wasPressedThisFrame) session.AddToBag(session.Products[8]);
            if (keyboard.digit4Key.wasPressedThisFrame) session.AddToBag(session.Products[9]);
        }
        private void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
