using UnityEngine;
using System.Collections;

// Определяем перечисление вне класса, но не в пространстве имен
public enum CharacterClass
{
    Warrior,    // Воин (ближний бой)
    Archer,     // Стрелок (дальний бой)
    Defender,   // Защитник (танк) - оставляем для совместимости
    Assassin,   // Убийца (высокий урон)
    Support,    // Поддержка (лечение)
    Specialist, // Специалист (особые умения)
    Tank,       // Танк (высокое здоровье)
    Evader      // Уклонист (отскок и яд)
}

public class BaseCharacter : MonoBehaviour
{
    // Новое поле для контроля боевого состояния
public bool isBattleReady = false;

    // Флаг, определяющий, является ли персонаж врагом
    public bool isEnemy = false; // По умолчанию персонаж является союзником
    
    // Основные характеристики
    public string characterName = "Character";
    public CharacterClass characterClass;
    public float maxHealth = 100f;
    public float currentHealth;
    public float attackDamage = 10f;
    public float attackSpeed = 0.5f; // Атаки в секунду
    public float moveSpeed = 1.5f;
    public float attackRange = 2f;
    
    // Дополнительные характеристики
    public float critChance = 0.05f; // 5% шанс крита
    public float critMultiplier = 2f; // Крит наносит в 2 раза больше урона
    
        // Система энергии
        public float maxEnergy = 100f;
        public float currentEnergy = 0f;
        public float energyRegenRate = 20f; // Единиц в секунду

        // Вот так должен выглядеть метод с телом
        public void UpdateHealthBar()
        {
            // Логика обновления полоски здоровья
            // Можно оставить пустым для начала
        }

        // Добавь эти строки в класс BaseCharacter
        public GameObject healthBarPrefab; // Назначь через инспектор
        private GameObject healthBarInstance;

    // Цель для атаки
    protected Transform target;
    
    // Время до следующей атаки
    protected float nextAttackTime = 0f;
    
    // Состояние персонажа
    protected bool isDead = false;
    
    // Инициализация
    protected virtual void Start()
    {
        currentHealth = maxHealth;
        if (healthBarPrefab != null)
{
    healthBarInstance = Instantiate(healthBarPrefab, transform);
    healthBarInstance.transform.localPosition = new Vector3(0, 2.0f, 0); // Размещение над персонажем
}
    }
    
 // Возможные состояния для персонажей
public enum CharacterState
{
    Idle,       // Ожидание (до начала боя)
    Ready,      // Готов к бою, но ещё не атакует
    Combat,     // Активный бой
    Dead        // Персонаж мёртв
}

// Текущее состояние персонажа
public CharacterState currentState = CharacterState.Idle;

// Обновление каждый кадр
// Добавь эти переменные на уровне класса (после всех других полей, но перед методами)
private Color originalColor = Color.white;
private bool hasOriginalColor = false;

// Обновление каждый кадр
protected virtual void Update()
{
    // Визуальное отображение состояния
    if (!hasOriginalColor && GetComponent<Renderer>() != null)
    {
        originalColor = GetComponent<Renderer>().material.color;
        hasOriginalColor = true;
    }

    // Визуальное отображение состояния
    if (GetComponent<Renderer>() != null)
    {
        Renderer rend = GetComponent<Renderer>();
        switch (currentState)
        {
            case CharacterState.Idle:
                rend.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
                break;
            case CharacterState.Ready:
                rend.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.8f);
                break;
            case CharacterState.Combat:
                rend.material.color = originalColor;
                break;
            case CharacterState.Dead:
                rend.material.color = new Color(originalColor.r * 0.5f, originalColor.g * 0.5f, originalColor.b * 0.5f, 0.7f);
                break;
        }
    }

    // Регенерация энергии только в боевом режиме
    if (currentEnergy < maxEnergy && currentState == CharacterState.Combat)
    {
        currentEnergy += energyRegenRate * Time.deltaTime;
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
    }
    
    // Проверка глобального статуса боя - обновление состояния
    if (BattleManager.IsFightStarted && currentState == CharacterState.Ready)
    {
        // Переходим в боевое состояние при старте боя
        currentState = CharacterState.Combat;
        Debug.Log($"{characterName} переходит в боевой режим!");
    }
    
    // СТРОГАЯ ПРОВЕРКА: Не выполняем боевые действия, если не в боевом режиме
    if (currentState != CharacterState.Combat)
    {
        return; // Ранний выход для всех персонажей не в боевом режиме
    }
    
    // Если персонаж мёртв, не выполняем боевую логику
    if (isDead)
    {
        currentState = CharacterState.Dead;
        return;
    }
    
    // БОЕВАЯ ЛОГИКА - только выполняется если currentState == CharacterState.Combat
    
    // Каждые 0.5 секунды обновляем цель на ближайшую
    if (Time.time % 0.5f < Time.deltaTime)
    {
        target = FindNearestTarget();
    }

    // Атака и перемещение
    if (target != null)
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        if (distanceToTarget <= attackRange)
        {
            if (CanAttack())
            {
                Attack(target);
            }
        }
        else
        {
            MoveTowards(target, attackRange);
        }
    }
}
    


    // Метод поиска ближайшей цели
    protected virtual Transform FindNearestTarget()
    {
        // Ищем цели из противоположной команды
        string targetTag = isEnemy ? "Player" : "Enemy";
        
        // Находим все возможные цели с нужным тегом
        GameObject[] possibleTargets = GameObject.FindGameObjectsWithTag(targetTag);
        float closestDistance = Mathf.Infinity;
        Transform nearest = null;
        
        // Перебираем все цели и ищем ближайшую, которая еще жива
        foreach (GameObject target in possibleTargets)
        {
            // Проверяем, активен ли объект и имеет ли он компонент BaseCharacter
            BaseCharacter targetChar = target.GetComponent<BaseCharacter>();
            if (target.activeInHierarchy && targetChar != null && targetChar.currentHealth > 0)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                // Обновляем ближайшую цель, если нашли более близкую
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearest = target.transform;
                }
            }
        }
        
        // Выводим информацию о выбранной цели
        if (nearest != null)
        {
            Debug.Log($"{characterName} переключился на новую цель: {nearest.name} на расстоянии {closestDistance}м");
        }
        else
        {
            Debug.Log($"{characterName} не может найти подходящую цель");
        }
        
        return nearest;
    }
    
    // Метод движения к цели
    protected virtual void MoveTowards(Transform destination, float stoppingDistance)
{
    if (destination == null) return;
    
    float distance = Vector3.Distance(
        new Vector3(transform.position.x, 0, transform.position.z),
        new Vector3(destination.position.x, 0, destination.position.z)
    );
    
    if (distance > stoppingDistance)
    {
        Vector3 direction = (destination.position - transform.position).normalized;
        direction.y = 0; // Исключаем движение по вертикали
        
        Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;
        newPosition.y = 0.5f; // Фиксируем высоту над землей
        transform.position = newPosition;
        
        transform.forward = direction; // Поворачиваем персонажа в направлении движения
    }
}
    
    // Метод проверки, можно ли атаковать
    protected virtual bool CanAttack()
    {
        return Time.time >= nextAttackTime && !isDead;
    }
    
    // Базовый метод атаки
    protected virtual void Attack(Transform attackTarget)
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
        
        // Получаем компонент BaseCharacter цели
        BaseCharacter targetCharacter = attackTarget.GetComponent<BaseCharacter>();
        if (targetCharacter != null)
        {
            targetCharacter.TakeDamage(damage);
        }
        
        // Визуальный эффект атаки
        StartCoroutine(AttackAnimation());
    }
    
    // Метод получения урона
    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        Debug.Log($"{characterName} получает {damage} урона. Осталось {currentHealth} HP");
        
        // Визуальный эффект получения урона
        StartCoroutine(DamageVisualEffect());
        
        // Проверка на смерть
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    // Метод смерти
    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"{characterName} погибает!");
        
        // Отключаем коллайдер
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Анимация смерти или другие эффекты
        StartCoroutine(DeathAnimation());
    }
    
    // Анимация атаки
    protected virtual IEnumerator AttackAnimation()
    {
        // В базовом классе просто делаем паузу
        // Дочерние классы могут переопределить с визуальными эффектами
        yield return new WaitForSeconds(0.2f);
    }
    
    // Визуальный эффект получения урона
    protected virtual IEnumerator DamageVisualEffect()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
        else
        {
            yield return null;
        }
    }
    
    // Анимация смерти
    protected virtual IEnumerator DeathAnimation()
    {
        // В базовом классе просто скрываем объект
        yield return new WaitForSeconds(1.0f);
        gameObject.SetActive(false);
    }
    // В классе BaseCharacter или дочернем классе Assassin
    protected Transform FindTarget()
    {
        // Ищем всех возможных противников
        GameObject[] possibleTargets;
        
        if (isEnemy)
        {
            possibleTargets = GameObject.FindGameObjectsWithTag("Player");
        }
        else
        {
            possibleTargets = GameObject.FindGameObjectsWithTag("Enemy");
        }
        
        // Находим ближайшего противника
        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (GameObject target in possibleTargets)
        {
            if (target == null || !target.activeInHierarchy || target.GetComponent<BaseCharacter>().currentHealth <= 0)
                continue;
                
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target.transform;
            }
        }
        
        Debug.Log($"{gameObject.name} выбрал цель: {(closestTarget ? closestTarget.name : "никого")} на дистанции {closestDistance}");
        
        return closestTarget;
    }
    // Добавь это в BaseCharacter.cs

public void SetState(CharacterState newState)
{
    CharacterState oldState = currentState;
    currentState = newState;
    
    // Логирование изменения состояния
    Debug.Log($"{characterName} меняет состояние: {oldState} -> {newState}");
    
    // Особые действия при смене состояния
    if (newState == CharacterState.Combat && oldState != CharacterState.Combat)
    {
        // Сброс целей при входе в боевой режим
        target = null;
    }
    else if (newState == CharacterState.Dead)
    {
        // Убедимся, что персонаж действительно "мёртв"
        isDead = true;
    }
}
}