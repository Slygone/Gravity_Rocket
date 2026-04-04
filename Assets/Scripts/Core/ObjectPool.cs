using System.Collections.Generic;
using UnityEngine;

namespace GravityRocket.Core
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialSize = 10;

        private readonly Queue<GameObject> _pool = new();
        private Transform _poolParent;

        private void Awake()
        {
            _poolParent = new GameObject($"Pool_{_prefab.name}").transform;
            _poolParent.SetParent(transform);

            for (int i = 0; i < _initialSize; i++)
            {
                CreateInstance();
            }
        }

        private void CreateInstance()
        {
            var obj = Instantiate(_prefab, _poolParent);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }

        public GameObject Get()
        {
            if (_pool.Count == 0)
            {
                CreateInstance();
            }

            var obj = _pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(_poolParent);
            _pool.Enqueue(obj);
        }

        public void ReturnAll()
        {
            var toReturn = new List<GameObject>();
            for (int i = 0; i < _poolParent.childCount; i++)
            {
                var child = _poolParent.GetChild(i).gameObject;
                if (child.activeSelf)
                {
                    toReturn.Add(child);
                }
            }
            foreach (var obj in toReturn)
            {
                Return(obj);
            }
        }
    }
}
