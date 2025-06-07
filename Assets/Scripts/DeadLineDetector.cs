using UnityEngine;

public class DeadLineDetector : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Object"))
        {
            Debug.Log("게임 오버");
        }
    }

}
