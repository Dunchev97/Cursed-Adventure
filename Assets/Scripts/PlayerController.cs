using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Скорость перемещения персонажа
    public float moveSpeed = 5f;
    
    // Компонент Character Controller
    private CharacterController controller;
    
    // Вызывается при старте
    void Start()
    {
        // Получаем компонент CharacterController
        controller = GetComponent<CharacterController>();
    }
    
    // Вызывается каждый кадр
    void Update()
    {
        // Получаем ввод от игрока (клавиши WASD или стрелки)
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        // Создаем вектор направления движения
        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput);
        
        // Если есть ввод от игрока
        if (moveDirection.magnitude > 0.1f)
        {
            // Поворачиваем персонажа в направлении движения
            transform.forward = moveDirection;
            
            // Перемещаем персонажа
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);
        }
    }
}