using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Цель, за которой следует камера (игрок)
    public Transform target;
    
    // Смещение камеры относительно игрока
    public Vector3 offset = new Vector3(0, 10, -10);
    
    // Скорость сглаживания движения камеры
    public float smoothSpeed = 5f;
    
    void LateUpdate()
    {
        // Если цель не указана, выходим
        if (target == null)
            return;
            
        // Рассчитываем позицию, куда должна переместиться камера
        Vector3 desiredPosition = target.position + offset;
        
        // Плавно перемещаем камеру к этой позиции
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // Камера всегда смотрит на игрока
        transform.LookAt(target);
    }
}