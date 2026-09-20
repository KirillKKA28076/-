using UnityEngine;
namespace WarmBread
{
    public sealed class PassingCar : MonoBehaviour
    {
        private void Update()
        {
            transform.Translate(Vector3.right * (3.5f * Time.deltaTime));
            if (transform.localPosition.x > 35) transform.localPosition = new Vector3(-35,0,11);
        }
    }
}
