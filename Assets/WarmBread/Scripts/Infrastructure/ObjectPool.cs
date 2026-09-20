using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarmBread
{
    public sealed class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(1)] private int initialSize = 8;
        [SerializeField, Min(1)] private int maxSize = 32;
        [SerializeField] private Transform container;

        private readonly Queue<GameObject> available = new Queue<GameObject>();
        private readonly HashSet<GameObject> allObjects = new HashSet<GameObject>();

        private void Awake()
        {
            if (container == null) container = transform;
            for (var i = 0; i < initialSize; i++) Create();
        }

        public GameObject Rent(Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            if (available.Count == 0 && allObjects.Count < maxSize) Create();
            if (available.Count == 0) return null;

            var instance = available.Dequeue();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public void Return(GameObject instance)
        {
            if (instance == null || !allObjects.Contains(instance)) return;
            instance.SetActive(false);
            instance.transform.SetParent(container);
            available.Enqueue(instance);
        }

        private void Create()
        {
            if (prefab == null || allObjects.Count >= maxSize) return;
            var instance = Instantiate(prefab, container);
            instance.SetActive(false);
            allObjects.Add(instance);
            available.Enqueue(instance);
        }

        private void OnDestroy()
        {
            available.Clear();
            allObjects.Clear();
        }
    }
}
