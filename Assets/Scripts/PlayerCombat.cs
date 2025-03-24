using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    // Урон от атаки игрока
    public float attackDamage = 20f;
    
    // Дистанция атаки
    public float attackRange = 2f;
    
    // Ссылка на объект оружия (меч или другое оружие)
    public Transform weapon;

    // Продолжительность анимации атаки
    public float attackDuration = 0.3f;

    // Флаг, указывающий идет ли анимация атаки
    private bool isAttacking = false;

    // Исходное положение оружия
    private Vector3 originalWeaponPosition;
    private Quaternion originalWeaponRotation;

    void Start()
{
    // Сохраняем исходное положение оружия
    if (weapon != null)
    {
        originalWeaponPosition = weapon.localPosition;
        originalWeaponRotation = weapon.localRotation;
    }
    
    // ВАЖНО: Отключаем возможность атаковать врагов без тега Enemy
    Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Default"), true);
}
    
    // Вызывается каждый кадр
    void Update()
    {
        // Если нажата кнопка атаки и анимация атаки не идет
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartCoroutine(AttackAnimation());
        }
    }
    
    // Метод атаки
    void Attack()
    {
        // Выводим сообщение для отладки
        Debug.Log("Попытка атаки!");
        
        // Ищем всех врагов в радиусе атаки
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        
        // Проходим по всем найденным коллайдерам
        foreach (Collider collider in hitColliders)
        {
            // Если нашли врага
            EnemyController enemy = collider.GetComponent<EnemyController>();
            if (enemy != null)
            {
                // Наносим урон
                enemy.TakeDamage(attackDamage);
                Debug.Log("Атаковали врага!");
            }
        }
        
        // Визуализация области атаки (для отладки)
        Debug.DrawRay(transform.position, transform.forward * attackRange, Color.red, 1.0f);
    }

    // Корутина для анимации атаки
    IEnumerator AttackAnimation()
    {
        // Устанавливаем флаг атаки
        isAttacking = true;
        
        // Выполняем атаку
        Attack();
        
        // Если есть оружие, анимируем его
        if (weapon != null)
        {
            // Начальная позиция анимации
            weapon.localRotation = Quaternion.Euler(0, 0, -45);
            
            // Середина анимации
            float elapsed = 0f;
            while (elapsed < attackDuration / 2)
            {
                elapsed += Time.deltaTime;
                weapon.localRotation = Quaternion.Euler(0, 0, -45 + 90 * (elapsed / (attackDuration / 2)));
                yield return null;
            }
            
            // Возврат в исходное положение
            elapsed = 0f;
            while (elapsed < attackDuration / 2)
            {
                elapsed += Time.deltaTime;
                weapon.localRotation = Quaternion.Euler(0, 0, 45 - 45 * (elapsed / (attackDuration / 2)));
                yield return null;
            }
            
            // Возвращаем оружие в исходное положение
            weapon.localPosition = originalWeaponPosition;
            weapon.localRotation = originalWeaponRotation;
        }
        else
        {
            // Если оружия нет, просто ждем завершения анимации
            yield return new WaitForSeconds(attackDuration);
        }
        
        // Снимаем флаг атаки
        isAttacking = false;
    }
}