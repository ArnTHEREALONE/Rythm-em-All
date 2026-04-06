using UnityEngine;

public class Slow : MonoBehaviour
{
    public float multiplicator, timer, duration, baseSpeed, slowSpeed;

    void Start()
    {
        multiplicator = baseSpeed;
    }

    public void ApplySlow()
    {
        multiplicator = slowSpeed;
        timer = 0f;
    }

    void Update()
    {
        if (multiplicator != baseSpeed)
        {
            timer += Time.deltaTime;
        }

        if (timer >= duration)
        {
            multiplicator = baseSpeed;
        }

        if (multiplicator == baseSpeed)
        {
            timer = 0f;
        }
    }
}
