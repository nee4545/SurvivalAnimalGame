
using UnityEngine;

public class DestroyerAfterDelay : MonoBehaviour
{
    public float delay =0.1f;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(this.gameObject, delay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
