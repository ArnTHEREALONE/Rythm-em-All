using UnityEditorInternal;
using UnityEngine;

public class Multiplicator : MonoBehaviour
{
    [Header("script affected by multiplicator")]
    public MonoBehaviour[] timeControls;
    public float multiplicatorGlobal, p, g, m;
    void Start()
    {
        multiplicatorGlobal = 1f;
    }

    void Update()
    {

    }

    public void Perfect()
    {
        multiplicatorGlobal += p;
        Debug.Log("Perfect ! multiplicator : " + multiplicatorGlobal);
    }

    public void Good()
    {
        multiplicatorGlobal += g;
        Debug.Log("Good ! multiplicator : " + multiplicatorGlobal);
    }

    public void Missed()
    {
        multiplicatorGlobal = m;
        Debug.Log("Missed ! multiplicator : " + multiplicatorGlobal);
    }
}
