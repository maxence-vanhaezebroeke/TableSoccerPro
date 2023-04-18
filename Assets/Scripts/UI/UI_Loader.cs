using UnityEngine;

public class UI_Loader : MonoBehaviour
{
    private readonly Vector3 ROTATION_VECTOR = new Vector3(0f, 0f, -35f);

    public void Display()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameObject.activeSelf)
        {
            transform.Rotate(ROTATION_VECTOR * Time.deltaTime);
        }
    }
}
