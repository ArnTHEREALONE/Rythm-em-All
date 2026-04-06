using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FollowTarget : MonoBehaviour
{
    public Transform target;
    public float speed;

    Slow slow;

    void Start()
    {
        slow = GetComponent<Slow>();

        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * slow.multiplicator * Time.deltaTime;
    }
}
