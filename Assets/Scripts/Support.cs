using UnityEngine;
using System.Collections;

public class Support : BaseCharacter
{
    // Специальные характеристики поддержки
    public float healAmount = 40f; // Количество лечения согласно таблице
    public float healCooldown = 5f;
    public float healRange = 6f;
    private float nextHealTime = 0f;
    
    public float buffAmount = 1.5f; // Множитель усиления
    public float buffDuration = 10f;
    public float buffCooldown = 15f;
    private float nextBuffTime = 0f;
    public float safeDistance = 4f; // Безопасная дистанция от врагов
    
    // Переменные для отслеживания движения
    private Vector3 lastPosition;
    private bool isMoving = false;
    private float stationaryThreshold = 0.05f; // Порог для определения, что персонаж стоит на месте
    
    // Инициализация
    protected override void Start()
    {
        base.Start();
        characterClass = CharacterClass.Support;
        characterName = "Поддержка";
        
        // Характеристики поддержки по таблице баланса
        maxHealth = 80f;
        currentHealth = maxHealth;
        attackDamage = 12f;
        attackSpeed = 0.7f;
        attackRange = 5f;
        moveSpeed = 3.2f;
        
        // Обновляем шанс крита согласно таблице
        critChance = 0.1f;
        critMultiplier = 2f;
        
        // Инициализируем lastPosition
        lastPosition = transform.position;
    }
    
    // Обновление каждый кадр
    protected override void Update()
    {
        // Определяем, движется ли персонаж
        isMoving = Vector3.Distance(transform.position, lastPosition) > stationaryThreshold;
        lastPosition = transform.position;
        
        // Вызываем базовый метод Update для обработки общей логики и состояния
        base.Update();
        
        // Выходим, если персонаж не в боевом режиме
        if (currentState != CharacterState.Combat) return;
        if (isDead) return;
        
        // Лечение и бафф можно применять в движении
        // Пытаемся найти союзников, нуждающихся в лечении
        if (Time.time >= nextHealTime && currentEnergy >= 30f)
        {
            BaseCharacter allyToHeal = FindAllyToHeal();
            if (allyToHeal != null)
            {
                HealAlly(allyToHeal);
            }
        }
        
        // Пытаемся усилить союзников
        if (Time.time >= nextBuffTime && currentEnergy >= 50f)
        {
            BaseCharacter allyToBuff = FindAllyToBuff();
            if (allyToBuff != null)
            {
                BuffAlly(allyToBuff);
            }
        }
        
        // Добавляем логику для атаки врагов
        // Находим ближайшего врага
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            target = FindNearestTarget();
        }
        
        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            // Если враг слишком близко, отходим и не атакуем
            if (distanceToTarget < safeDistance)
            {
                Vector3 directionAway = (transform.position - target.position).normalized;
                transform.position += directionAway * moveSpeed * Time.deltaTime;
                transform.forward = -directionAway; // Продолжаем смотреть на врага
            }
            // Если цель в оптимальной дистанции атаки - атакуем только стоя на месте
            else if (distanceToTarget <= attackRange && distanceToTarget >= safeDistance)
            {
                // Поворачиваемся к цели
                transform.forward = (target.position - transform.position).normalized;
                
                // Атакуем только если стоим на месте
                if (!isMoving && CanAttack())
                {
                    Attack(target);
                }
            }
            else
            {
                // Если нет приоритетных действий (лечение или усиление), двигаемся к цели
                BaseCharacter allyToHeal = FindAllyToHeal();
                BaseCharacter allyToBuff = FindAllyToBuff();
                
                // Проверяем, доступно ли лечение и есть ли цель для него
                bool canHeal = Time.time >= nextHealTime && currentEnergy >= 30f && allyToHeal != null;
                
                // Проверяем, доступно ли усиление и есть ли цель для него
                bool canBuff = Time.time >= nextBuffTime && currentEnergy >= 50f && allyToBuff != null;
                
                // Если обе способности на перезарядке или нет доступных целей для них,
                // двигаемся к цели для оптимальной дистанции атаки
                if ((!canHeal && !canBuff) || (allyToHeal == null && allyToBuff == null))
                {
                    MoveTowards(target, safeDistance);
                }
            }
        }
    }
    
    // Поиск союзника, нуждающегося в лечении
    private BaseCharacter FindAllyToHeal()
    {
        BaseCharacter[] allies = FindObjectsOfType<BaseCharacter>();
        BaseCharacter mostInjuredAlly = null;
        float lowestHealthPercentage = 0.8f; // Лечим только если здоровье ниже 80%
        
        foreach (BaseCharacter ally in allies)
        {
            // Пропускаем себя, врагов и мертвых
            if (ally == this || ally.isEnemy || ally.currentHealth <= 0)
                continue;
            
            float healthPercentage = ally.currentHealth / ally.maxHealth;
            float distance = Vector3.Distance(transform.position, ally.transform.position);
            
            // Проверяем, находится ли союзник в зоне лечения и нуждается ли в лечении
            if (distance <= healRange && healthPercentage < lowestHealthPercentage)
            {
                lowestHealthPercentage = healthPercentage;
                mostInjuredAlly = ally;
            }
        }
        
        return mostInjuredAlly;
    }
    
    // Лечение союзника
    private void HealAlly(BaseCharacter ally)
    {
        nextHealTime = Time.time + healCooldown;
        currentEnergy -= 30f;
        
        Debug.Log($"{characterName} лечит {ally.characterName}!");
        
        // Увеличиваем здоровье, но не больше максимального
        ally.currentHealth += healAmount;
        ally.currentHealth = Mathf.Min(ally.currentHealth, ally.maxHealth);
        
        // Визуальный эффект лечения
        StartCoroutine(HealVisualEffect(ally.transform));
    }
    
    // Поиск союзника для наложения усиления
    private BaseCharacter FindAllyToBuff()
    {
        // В простом прототипе просто найдем ближайшего воина или стрелка
        BaseCharacter[] allies = FindObjectsOfType<BaseCharacter>();
        BaseCharacter allyToBuff = null;
        float closestDistance = healRange;
        
        foreach (BaseCharacter ally in allies)
        {
            // Пропускаем себя, врагов и мертвых
            if (ally == this || ally.isEnemy || ally.currentHealth <= 0)
                continue;
            
            // Предпочитаем воинов и стрелков для баффа
            if (ally.characterClass == CharacterClass.Warrior || ally.characterClass == CharacterClass.Archer)
            {
                float distance = Vector3.Distance(transform.position, ally.transform.position);
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    allyToBuff = ally;
                }
            }
        }
        
        return allyToBuff;
    }
    
    // Наложение усиления на союзника
    private void BuffAlly(BaseCharacter ally)
    {
        nextBuffTime = Time.time + buffCooldown;
        currentEnergy -= 50f;
        
        Debug.Log($"{characterName} усиливает {ally.characterName}!");
        
        // Временно увеличиваем урон
        ally.attackDamage *= buffAmount;
        
        // Запускаем корутину для отмены эффекта
        StartCoroutine(RemoveBuffAfterDuration(ally));
        
        // Визуальный эффект усиления
        StartCoroutine(BuffVisualEffect(ally.transform));
    }
    
    // Корутина для отмены эффекта усиления
    private IEnumerator RemoveBuffAfterDuration(BaseCharacter ally)
    {
        yield return new WaitForSeconds(buffDuration);
        
        if (ally != null && ally.gameObject.activeInHierarchy)
        {
            ally.attackDamage /= buffAmount;
            Debug.Log($"Усиление {ally.characterName} закончилось");
        }
    }
    
    // Визуальный эффект лечения
    private IEnumerator HealVisualEffect(Transform allyTransform)
    {
        // Создаем временный объект для визуализации эффекта
        GameObject healEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        healEffect.transform.position = allyTransform.position + Vector3.up;
        healEffect.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        // Убираем коллайдер
        Destroy(healEffect.GetComponent<Collider>());
        
        // Устанавливаем материал
        Material healMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        healMaterial.color = new Color(0, 1, 0, 0.5f); // Полупрозрачный зеленый
        healEffect.GetComponent<Renderer>().material = healMaterial;
        
        // Анимируем эффект
        float duration = 1f;
        float elapsed = 0;
        
        Vector3 startScale = healEffect.transform.localScale;
        Vector3 endScale = startScale * 2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Увеличиваем размер
            healEffect.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            // Изменяем прозрачность
            healMaterial.color = new Color(0, 1, 0, 0.5f * (1 - t));
            
            yield return null;
        }
        
        // Уничтожаем эффект
        Destroy(healEffect);
    }
    
    // Визуальный эффект усиления
    private IEnumerator BuffVisualEffect(Transform allyTransform)
    {
        // Создаем временный объект для визуализации эффекта
        GameObject buffEffect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        buffEffect.transform.position = allyTransform.position;
        buffEffect.transform.localScale = new Vector3(1f, 0.1f, 1f);
        
        // Убираем коллайдер
        Destroy(buffEffect.GetComponent<Collider>());
        
        // Устанавливаем материал
        Material buffMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        buffMaterial.color = new Color(1, 0.8f, 0, 0.5f); // Полупрозрачный золотой
        buffEffect.GetComponent<Renderer>().material = buffMaterial;
        
        // Анимируем эффект
        float duration = 1.5f;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Вращаем эффект
            buffEffect.transform.Rotate(0, 360f * Time.deltaTime, 0);
            
            // Следуем за целью
            buffEffect.transform.position = new Vector3(
                allyTransform.position.x,
                allyTransform.position.y + 0.1f,
                allyTransform.position.z
            );
            
            yield return null;
        }
        
        // Уничтожаем эффект
        Destroy(buffEffect);
    }
}