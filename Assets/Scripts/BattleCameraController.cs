using UnityEngine;

public class BattleCameraController : MonoBehaviour
{
    // Позиция и поворот камеры для наблюдения за боем
    public Vector3 targetPosition = new Vector3(0, 15, -15);
    public Vector3 targetRotation = new Vector3(45, 0, 0);
    
    void Start()
    {
        // Устанавливаем позицию и поворот камеры
        transform.position = targetPosition;
        transform.rotation = Quaternion.Euler(targetRotation);
    }
}