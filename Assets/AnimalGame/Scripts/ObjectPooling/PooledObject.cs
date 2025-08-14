
using UnityEngine;

public class PooledObject : MonoBehaviour
{
    
        [HideInInspector] public GameObject prefab; // set by PoolManager
        public void Despawn(float delay = 0f)
        {
            if (delay <= 0f) PoolManager.Despawn(gameObject);
            else StartCoroutine(DespawnAfter(delay));
        }

        private System.Collections.IEnumerator DespawnAfter(float t)
        {
            yield return new WaitForSeconds(t);
            PoolManager.Despawn(gameObject);
        }
}
