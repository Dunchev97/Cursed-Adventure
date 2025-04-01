using UnityEngine;
using System.Collections;

public class Assassin : BaseCharacter
{
    // Специальные характеристики убийцы
    public float backstabMultiplier = 1.5f; // Множитель урона при атаке сзади (согласно таблице)
    
    // Расстояние рывка
    public float dashDistance = 5f; 
    public float dashCooldown = 4f; // Перезарядка рывка
    
    // Время следующего рывка
    private float nextDashTime = 0f;
    
    // Инициализация
    protected override void Start()
    {
        base.Start();
        characterClass = CharacterClass.Assassin;
        characterName = "Убийца";
        
        // Характеристики убийцы по таблице баланса
        maxHealth = 85f;
        currentHealth = maxHealth;
        attackDamage = 18f;
        attackSpeed = 1.0f;
        attackRange = 2f;
        moveSpeed = 3.6f;
        
        // Обновляем шанс крита согласно таблице
        critChance = 0.2f;
        critMultiplier = 2f;
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
            
            // Если цель достаточно далеко и можно использовать рывок
            if (distanceToTarget > attackRange && distanceToTarget < dashDistance + attackRange && Time.time >= nextDashTime && currentEnergy >= 100f)
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
    
    // Способность рывка к цели
    private void Dash()
    {
        if (target == null) return;
        
        nextDashTime = Time.time + dashCooldown;
        currentEnergy -= 100f;
        
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