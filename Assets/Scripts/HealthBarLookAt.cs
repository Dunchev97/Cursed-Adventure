// Добавьте новый скрипт для полосок здоровья
using UnityEngine;

public class HealthBarLookAt : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        if (mainCamera != null)
        {
            // Поворачиваем Canvas к камере
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward);
        }
    }
}