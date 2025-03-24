using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    // Ссылка на менеджер боя для проверки состояния
    private BattleManager battleManager;
    
    // Компонент с характеристиками персонажа
    private BaseCharacter characterStats;
    
    // Цель для атаки
    private Transform target;
    
    // Параметры атаки и движения
    private float attackCooldown = 0f;
    private float attackRange;
    private float moveSpeed;
    private float attackSpeed;
    
    // Задержка поиска цели для оптимизации
    private float findTargetCooldown = 0f;
    private const float FIND_TARGET_INTERVAL = 0.5f;
    
    // Состояние врага
    private enum EnemyState { Idle, Chasing, Attacking }
    private EnemyState currentState = EnemyState.Idle;
    
    // Флаг для проверки, начал ли этот враг боевые действия
    private bool hasActivated = false;
    
    void Awake()
    {
        // Принудительно отключаем компонент при создании
        enabled = false;
    }
    
    void OnEnable()
    {
        Debug.Log($"EnemyController для {gameObject.name} АКТИВИРОВАН.");
        
        // Находим менеджер боя в сцене
        battleManager = FindObjectOfType<BattleManager>();
        
        // Получаем компонент базового персонажа
        characterStats = GetComponent<BaseCharacter>();
        
        // Инициализируем параметры из характеристик персонажа
        if (characterStats != null)
        {
            attackRange = characterStats.attackRange;
            moveSpeed = characterStats.moveSpeed;
            attackSpeed = characterStats.attackSpeed;
        }
        else
        {
            // Значения по умолчанию, если компонент не найден
            attackRange = 1.5f;
            moveSpeed = 3.0f;
            attackSpeed = 1.0f;
        }
        
        // Запускаем корутину для периодического обновления целей
        StartCoroutine(UpdateEnemyState());
    }
    
    void Update()
    {
        // Базовая проверка боевой готовности
        if (!BattleManager.IsFightStarted || characterStats == null || !characterStats.isBattleReady)
        {
            return; // Выходим из метода, если бой не начат
        }
        
        // Дополнительная проверка начала боя
        if (!hasActivated)
        {
            hasActivated = true;
            Debug.Log($"Враг {gameObject.name} начинает боевые действия!");
        }
        
        // Обрабатываем кулдауны
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }
        
        if (findTargetCooldown > 0)
        {
            findTargetCooldown -= Time.deltaTime;
        }
        
        // Обработка поведения в зависимости от состояния
        switch (currentState)
        {
            case EnemyState.Idle:
                // В режиме ожидания периодически ищем цель
                if (findTargetCooldown <= 0)
                {
                    FindTarget();
                    findTargetCooldown = FIND_TARGET_INTERVAL;
                }
                break;
                
            case EnemyState.Chasing:
                // Проверяем, что цель все еще существует
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    FindTarget();
                    if (target == null)
                    {
                        currentState = EnemyState.Idle;
                        return;
                    }
                }
                
                // Двигаемся к цели
                MoveToTarget();
                
                // Если достигли дистанции атаки, переключаемся на атаку
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                if (distanceToTarget <= attackRange)
                {
                    currentState = EnemyState.Attacking;
                }
                break;
                
            case EnemyState.Attacking:
                // Проверяем, что цель все еще существует
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    FindTarget();
                    if (target == null)
                    {
                        currentState = EnemyState.Idle;
                        return;
                    }
                }
                
                // Если цель вышла за пределы дистанции атаки, преследуем её
                float currentDistance = Vector3.Distance(transform.position, target.position);
                if (currentDistance > attackRange)
                {
                    currentState = EnemyState.Chasing;
                    return;
                }
                
                // Поворачиваемся к цели
                LookAtTarget();
                
                // Атакуем, если кулдаун прошел
                if (attackCooldown <= 0)
                {
                    Attack();
                }
                break;
        }
    }
    
    // Корутина для периодического обновления состояния
    private IEnumerator UpdateEnemyState()
    {
        yield return new WaitForSeconds(2.0f); // Начальная задержка
        
        while (true)
        {
            // Проверка: если бой не начался или персонаж не готов, ждем
            if (!BattleManager.IsFightStarted || characterStats == null || !characterStats.isBattleReady)
            {
                yield return new WaitForSeconds(1.0f);
                continue;
            }
            
            // Если у нас нет цели, ищем новую
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                FindTarget();
                
                if (target != null)
                {
                    // Если цель найдена, начинаем преследование
                    currentState = EnemyState.Chasing;
                }
                else
                {
                    // Если цель не найдена, остаемся в режиме ожидания
                    currentState = EnemyState.Idle;
                }
            }
            
            yield return new WaitForSeconds(1.0f);
        }
    }
    
    // Поиск ближайшей цели среди игроков
    private void FindTarget()
    {
        // Дополнительная проверка: если бой не начался, не ищем цель
        if (!BattleManager.IsFightStarted || !characterStats.isBattleReady)
        {
            target = null;
            return;
        }
        
        // Ищем всех игроков
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        // Находим ближайшего активного игрока
        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (GameObject player in players)
        {
            // Проверяем, что игрок активен и жив
            BaseCharacter playerCharacter = player.GetComponent<BaseCharacter>();
            if (player.activeInHierarchy && playerCharacter != null && playerCharacter.currentHealth > 0)
            {
                // Рассчитываем дистанцию
                float distance = Vector3.Distance(transform.position, player.transform.position);
                
                // Обновляем ближайшую цель
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = player.transform;
                }
            }
        }
        
        // Устанавливаем найденную цель
        target = closestTarget;
        
        // Выводим отладочную информацию
        if (target != null)
        {
            Debug.Log($"{gameObject.name} нашел цель: {target.name} на расстоянии {closestDistance}");
        }
    }
    
    // Движение к цели
    private void MoveToTarget()
    {
        if (target == null)
            return;
            
        // Получаем направление к цели
        Vector3 direction = (target.position - transform.position).normalized;
        
        // Обнуляем Y-компоненту, чтобы двигаться только по горизонтали
        direction.y = 0;
        
        // Поворачиваемся к цели
        LookAtTarget();
        
        // Двигаемся в направлении цели
        transform.position += direction * moveSpeed * Time.deltaTime;
    }
    
    // Поворот к цели
    private void LookAtTarget()
    {
        if (target == null)
            return;
            
        // Запоминаем текущую Y-позицию
        float currentY = transform.position.y;
        
        // Создаем точку для поворота на той же высоте
        Vector3 targetPosition = new Vector3(target.position.x, currentY, target.position.z);
        
        // Плавно поворачиваемся к цели
        transform.LookAt(targetPosition);
    }
    
    // Атака цели
    private void Attack()
    {
        // Еще одна проверка для уверенности
        if (!BattleManager.IsFightStarted || !characterStats.isBattleReady)
        {
            return;
        }
        
        if (target == null || characterStats == null)
            return;
            
        // Получаем компонент BaseCharacter цели
        BaseCharacter targetCharacter = target.GetComponent<BaseCharacter>();
        
        if (targetCharacter != null)
        {
            // Наносим урон
            float damage = characterStats.attackDamage;
            
            // Проверка на критический удар
            bool isCritical = Random.value < characterStats.critChance;
            if (isCritical)
            {
                damage *= characterStats.critMultiplier;
            }
            
            // Применяем урон к цели
            targetCharacter.TakeDamage(damage);
            
            // Выводим отладочную информацию
            Debug.Log($"{gameObject.name} атакует {target.name} и наносит {damage} урона" + (isCritical ? " (КРИТ!)" : ""));
            
            // Устанавливаем кулдаун атаки
            attackCooldown = 1.0f / attackSpeed;
        }
    }
    
    // Метод для получения урона
    public void TakeDamage(float damage)
    {
        // Получаем компонент BaseCharacter, если есть
        BaseCharacter baseChar = GetComponent<BaseCharacter>();
        
        // Если есть BaseCharacter, передаем урон ему
        if (baseChar != null)
        {
            baseChar.TakeDamage(damage);
        }
        else
        {
            Debug.Log($"Враг получил {damage} урона, но не имеет компонента BaseCharacter");
        }
    }
    
    // Визуализация радиуса атаки в редакторе
    void OnDrawGizmosSelected()
    {
        // Отображаем радиус атаки в редакторе
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}