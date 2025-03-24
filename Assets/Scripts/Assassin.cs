using UnityEngine;
using System.Collections;

public class Assassin : BaseCharacter
{
    // Специальные характеристики убийцы
    public float backstabMultiplier = 2.5f; // Множитель урона при атаке сзади
    public float stealthCooldown = 12f; // Время восстановления невидимости
    public float stealthDuration = 5f; // Продолжительность невидимости
    public float dashDistance = 5f; // Расстояние рывка
    public float dashCooldown = 4f; // Перезарядка рывка
    
    private float nextStealthTime = 0f;
    private float nextDashTime = 0f;
    private bool isStealthed = false;
    
    // Инициализация
    protected override void Start()
    {
        base.Start();
        characterClass = CharacterClass.Assassin;
        characterName = "Убийца";
        
        // Характеристики убийцы
        maxHealth = 85f;
        currentHealth = maxHealth;
        attackDamage = 20f; // Высокий урон
        attackSpeed = 1.3f; // Быстрые атаки
        attackRange = 1.5f; // Короткая дистанция атаки
        moveSpeed = 4f; // Быстрое передвижение
        critChance = 0.2f; // Высокий шанс крита
        critMultiplier = 2.5f; // Сильный крит
    }
    
    // Обновление каждый кадр
protected override void Update()
{
    // Вызываем базовый метод Update для обработки общей логики и состояния
    base.Update();
    
    // Выходим, если персонаж не в боевом режиме
    if (currentState != CharacterState.Combat) return;
    if (isDead) return;
        
        // Находим ближайшего врага
if (target == null || !target.gameObject.activeInHierarchy)
{
    target = FindNearestTarget();
}
        
        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            // Если есть возможность использовать стелс и энергия позволяет
            if (!isStealthed && Time.time >= nextStealthTime && currentEnergy >= 60f && distanceToTarget > attackRange * 2f)
            {
                ActivateStealth();
            }
            
            // Если цель достаточно далеко и можно использовать рывок
            if (distanceToTarget > attackRange && distanceToTarget < dashDistance + attackRange && Time.time >= nextDashTime && currentEnergy >= 30f)
            {
                Dash();
            }
            // Если цель в пределах дальности атаки
            else if (distanceToTarget <= attackRange)
            {
                // Проверяем, атакуем ли мы сзади
                bool isBackstab = IsAttackingFromBehind();
                
                // Атакуем
                if (CanAttack())
                {
                    Attack(target, isBackstab);
                }
            }
            else
            {
                // Двигаемся к цели
                MoveTowards(target, attackRange);
            }
        }
    }
    
    // Переопределенный метод атаки для убийцы
    private void Attack(Transform attackTarget, bool isBackstab)
    {
        if (attackTarget == null || !CanAttack()) return;
        
        // Устанавливаем время следующей атаки
        nextAttackTime = Time.time + (1f / attackSpeed);
        
        // Определяем, будет ли критический удар
        bool isCritical = Random.value <= critChance;
        float damage = attackDamage;
        
        if (isCritical)
        {
            damage *= critMultiplier;
            Debug.Log($"{characterName} наносит критический удар!");
        }
        
        // Если атака сзади, увеличиваем урон
        if (isBackstab)
        {
            damage *= backstabMultiplier;
            Debug.Log($"{characterName} наносит удар в спину! Урон увеличен в {backstabMultiplier} раза.");
        }
        
        // Если мы в стелсе, выходим из него и наносим дополнительный урон
        if (isStealthed)
        {
            damage *= 1.5f;
            DeactivateStealth();
            Debug.Log($"{characterName} атакует из стелса! Дополнительный урон!");
        }
        
        // Наносим урон
        EnemyController targetEnemy = attackTarget.GetComponent<EnemyController>();
        if (targetEnemy != null)
        {
            targetEnemy.TakeDamage(damage);
        }
        
        // Визуальный эффект атаки
        StartCoroutine(AssassinAttackAnimation());
    }
    
    // Проверка, атакуем ли мы сзади
    private bool IsAttackingFromBehind()
    {
        if (target == null) return false;
        
        // Получаем направление от цели к убийце
        Vector3 targetToAssassin = transform.position - target.position;
        targetToAssassin.y = 0; // Игнорируем высоту
        
        // Получаем прямое направление цели
        Vector3 targetForward = target.forward;
        targetForward.y = 0; // Игнорируем высоту
        
        // Вычисляем угол между направлениями
        float angle = Vector3.Angle(targetToAssassin, targetForward);
        
        // Если угол меньше 60 градусов, считаем что атака происходит сзади
        return angle < 60f;
    }
    
    // Активация стелса
    private void ActivateStealth()
    {
        isStealthed = true;
        nextStealthTime = Time.time + stealthCooldown;
        currentEnergy -= 60f;
        
        Debug.Log($"{characterName} уходит в стелс!");
        
        // Делаем персонажа полупрозрачным
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = renderer.material.color;
            color.a = 0.3f; // Прозрачность
            renderer.material.color = color;
        }
        
        // Запускаем таймер для деактивации стелса
        StartCoroutine(DeactivateStealthAfterDuration());
    }
    
    // Деактивация стелса
    private void DeactivateStealth()
    {
        isStealthed = false;
        
        // Возвращаем нормальную видимость
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = renderer.material.color;
            color.a = 1f; // Полная непрозрачность
            renderer.material.color = color;
        }
    }
    
    // Корутина для автоматической деактивации стелса
    private IEnumerator DeactivateStealthAfterDuration()
    {
        yield return new WaitForSeconds(stealthDuration);
        
        if (isStealthed)
        {
            DeactivateStealth();
            Debug.Log($"{characterName} выходит из стелса.");
        }
    }
    
    // Способность рывка к цели
    private void Dash()
    {
        if (target == null) return;
        
        nextDashTime = Time.time + dashCooldown;
        currentEnergy -= 30f;
        
        Debug.Log($"{characterName} совершает рывок к цели!");
        
        // Направление к цели
        Vector3 direction = (target.position - transform.position).normalized;
        
        // Рассчитываем новую позицию
        Vector3 dashTarget = transform.position + direction * dashDistance;
        
        // Проверяем, не слишком ли близко к цели
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget < dashDistance)
        {
            // Если цель ближе, чем дистанция рывка, перемещаемся за цель
            dashTarget = target.position + direction * 1.5f;
        }
        
        // Выполняем рывок
        StartCoroutine(DashAnimation(dashTarget));
    }
    
    // Анимация рывка
    private IEnumerator DashAnimation(Vector3 targetPosition)
    {
        float dashDuration = 0.2f; // Быстрый рывок
        float elapsedTime = 0;
        Vector3 startPosition = transform.position;
        
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / dashDuration;
            
            // Используем кривую Эрмита для плавного начала и окончания движения
            float t = progress * progress * (3f - 2f * progress);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            yield return null;
        }
        
        // Убеждаемся, что персонаж достиг целевой позиции
        transform.position = targetPosition;
    }
    
    // Анимация атаки убийцы
    private IEnumerator AssassinAttackAnimation()
    {
        // Создаем визуальный след от удара
        GameObject slashEffect = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slashEffect.transform.position = transform.position + transform.forward * 0.7f + Vector3.up * 0.5f;
        slashEffect.transform.localScale = new Vector3(0.1f, 0.5f, 1.5f);
        slashEffect.transform.rotation = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 0, 45);
        
        // Убираем коллайдер
        Destroy(slashEffect.GetComponent<Collider>());
        
        // Устанавливаем материал
        Material slashMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        slashMaterial.color = new Color(1, 0, 0, 0.5f); // Полупрозрачный красный
        slashEffect.GetComponent<Renderer>().material = slashMaterial;
        
        // Анимируем след
        float duration = 0.15f;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1 - (elapsed / duration);
            
            slashMaterial.color = new Color(1, 0, 0, alpha * 0.5f);
            
            yield return null;
        }
        
        // Уничтожаем эффект
        Destroy(slashEffect);
    }
}