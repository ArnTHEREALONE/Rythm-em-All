using UnityEngine;

public class PlayerControler : MonoBehaviour
{
    public float speed;
    public Multiplicator m;

    private Vector3 moveInput;
    void Start()
    {

    }

    void Update()
    {
        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (moveInput.magnitude > 1f) moveInput.Normalize();

        Vector3 moveVelocity = moveInput * speed * m.multiplicatorGlobal;

        transform.Translate(moveVelocity * Time.deltaTime, Space.World);

        if (Input.GetButtonDown("Fire1"))
        {
            m.Perfect();
        }

        if (Input.GetButtonDown("Fire2"))
        {
            m.Missed();
        }
    }
}
