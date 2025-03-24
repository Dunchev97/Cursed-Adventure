using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image healthFill;
    public Image energyFill;
    
    private BaseCharacter character;
    private Camera mainCamera;
    
    void Start()
    {
        // Ищем BaseCharacter в родительской иерархии
        character = GetComponentInParent<BaseCharacter>();
        
        // Если не найден в родителях, ищем в корневом объекте
        if (character == null)
        {
            // Проверяем, является ли наш родитель дочерним объектом персонажа
            Transform parent = transform.parent;
            while (parent != null && character == null)
            {
                character = parent.GetComponent<BaseCharacter>();
                parent = parent.parent;
            }
        }
        
        // Если всё ещё не нашли, выводим ошибку, но не останавливаем выполнение
        if (character == null)
        {
            Debug.LogWarning("HealthBar не может найти компонент BaseCharacter в иерархии родителей", this);
            return; // Выходим из метода, чтобы избежать ошибок NullReferenceException
        }
        
        mainCamera = Camera.main;
        
        // Проверяем, найдены ли ссылки на компоненты UI
        if (healthFill == null)
        {
            Debug.LogWarning("Компонент HealthFill не назначен для HealthBar", this);
        }
        
        if (energyFill == null)
        {
            Debug.LogWarning("Компонент EnergyFill не назначен для HealthBar", this);
        }
    }
    
    void Update()
    {
        // Если не нашли персонажа или камеру, выходим
        if (character == null || mainCamera == null)
            return;
        
        // Обновляем заполнение полоски здоровья
        if (healthFill != null)
        {
            healthFill.fillAmount = character.currentHealth / character.maxHealth;
        }
        
        // Обновляем заполнение полоски энергии
        if (energyFill != null)
        {
            energyFill.fillAmount = character.currentEnergy / character.maxEnergy;
        }
        
        // Поворачиваем UI к камере
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward);
    }
}