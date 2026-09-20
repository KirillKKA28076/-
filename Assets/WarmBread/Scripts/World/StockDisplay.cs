using System.Collections.Generic;
using UnityEngine;

namespace WarmBread
{
    public sealed class StockDisplay : MonoBehaviour
    {
        private readonly List<GameObject> models = new List<GameObject>();
        private GameSession session;
        private ProductData product;
        private int lastCount = -1;

        public void Configure(GameSession owner, ProductData productData, int visualSlots = 4)
        {
            session = owner;
            product = productData;
            visualSlots = Mathf.Clamp(visualSlots, 1, 6);

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            models.Clear();
            for (var i = 0; i < visualSlots; i++)
            {
                var column = i % 2;
                var row = i / 2;
                var position = new Vector3(
                    (column - .5f) * .25f,
                    row * .06f,
                    row * .11f);

                var before = transform.childCount;
                WorldArt.Product(transform, product, position, .78f);
                if (transform.childCount > before)
                {
                    var model = transform.GetChild(transform.childCount - 1).gameObject;
                    model.name = "Остаток " + (i + 1) + " • " + (product != null ? product.Title : "товар");
                    models.Add(model);
                }
            }

            EventBus.Changed -= Refresh;
            EventBus.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            EventBus.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (session == null || product == null) return;
            var count = session.Stock != null ? session.Stock.Count(product.Id) : 0;
            if (count == lastCount) return;
            lastCount = count;

            var visible = 0;
            if (count > 0)
            {
                visible = Mathf.Clamp(Mathf.CeilToInt(count / 3f), 1, models.Count);
            }

            for (var i = 0; i < models.Count; i++)
            {
                if (models[i] != null) models[i].SetActive(i < visible);
            }
        }
    }
}
