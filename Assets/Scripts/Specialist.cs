using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Specialist : BaseCharacter
{
    // Специальные характеристики специалиста
    public float safeDistance = 3f;      // Безопасная дистанция от врагов
    public float slowPercentage = 0.2f;  // 20% замедление противника
    public float freezeRadius = 3f;      // Радиус заморозки
    public float freezeDuration = 2f;    // Длительность заморозки
    public float freezeCooldown = 8f;    // Перезарядка заморозки
    private float nextFreezeTime = 0f;
    private Dictionary<BaseCharacter, float> slowedEnemies = new Dictionary<BaseCharacter, float>();
    
    // Инициализация
    protected override void Start()
    {
        base.Start();
        characterClass = CharacterClass.Specialist;
        characterName = "Специалист";
        
        // Характеристики специалиста
        maxHealth = 85f;
        currentHealth = maxHealth;
        attackDamage = 6f;
        attackSpeed = 1.5f;
        attackRange = 3f;
        moveSpeed = 3.4f;
        
        // Шанс крита
        critChance = 0.1f;
        critMultiplier = 2f;
    }
    
    // Обновление каждый кадр
    protected override void Update()
    {
        // Вызываем базовый метод Update для обработки общей логики и состояния
        base.Update();
        
        // Выходим, если персонаж не в боевом режиме
        if (currentState != CharacterState.Combat) return;
        if (!gameObject.activeInHierarchy) return;
        
        // Обновляем список замедленных врагов
        UpdateSlowedEnemies();
        
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
            // Если есть несколько врагов рядом и заморозка готова
            else if (CountEnemiesInRadius(freezeRadius) >= 2 && Time.time >= nextFreezeTime && currentEnergy >= 100f)
            {
                FreezeArea();
            }
            // Если цель в пределах дальности атаки
            else if (distanceToTarget <= attackRange && distanceToTarget >= safeDistance)
            {
                // Поворачиваемся к цели и атакуем
                transform.forward = (target.position - transform.position).normalized;
                
                if (CanAttack())
                {
                    Attack(target);
                }
            }
            else
            {
                // Двигаемся к цели до оптимальной дистанции атаки
                MoveTowards(target, safeDistance);
            }
        }
    }
    
    // Переопределяем метод атаки для добавления эффекта замедления
    protected override void Attack(Transform attackTarget)
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
            // Наносим урон
            targetCharacter.TakeDamage(damage);
            
            // Добавляем эффект замедления
            SlowEnemy(targetCharacter, slowPercentage, 2f);
        }
        
        // Визуальный эффект атаки
        StartCoroutine(AttackAnimation());
    }
    
    // Заморозка области вокруг специалиста
    private void FreezeArea()
    {
        nextFreezeTime = Time.time + freezeCooldown;
        currentEnergy -= 100f;
        
        Debug.Log($"{characterName} создает поле заморозки!");
        
        // Находим всех врагов в радиусе заморозки
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, freezeRadius);
        foreach (Collider collider in hitColliders)
        {
            BaseCharacter enemy = collider.GetComponent<BaseCharacter>();
            if (enemy != null && enemy.isEnemy != isEnemy)
            {
                // Замораживаем каждого врага
                StartCoroutine(FreezeEnemy(enemy));
            }
        }
        
        // Визуальный эффект заморозки
        StartCoroutine(FreezeAreaEffect(transform.position, freezeRadius, freezeDuration));
    }
    
    // Подсчет врагов в радиусе
    private int CountEnemiesInRadius(float radius)
    {
        int count = 0;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        
        foreach (Collider collider in hitColliders)
        {
            BaseCharacter character = collider.GetComponent<BaseCharacter>();
            if (character != null && character.isEnemy != isEnemy)
            {
                count++;
            }
        }
        
        return count;
    }
    
    // Замедление врага
    private void SlowEnemy(BaseCharacter enemy, float slowAmount, float duration)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy) return;
        
        // Сохраняем исходную скорость, если это первое замедление
        if (!slowedEnemies.ContainsKey(enemy))
        {
            // Замедляем врага
            enemy.moveSpeed *= (1f - slowAmount);
            Debug.Log($"{enemy.characterName} замедлен на {slowAmount * 100}%");
            
            // Добавляем в словарь с временем окончания эффекта
            slowedEnemies[enemy] = Time.time + duration;
            
            // Визуальный эффект замедления
            StartCoroutine(SlowVisualEffect(enemy.transform, duration));
        }
        else
        {
            // Обновляем время окончания замедления
            slowedEnemies[enemy] = Time.time + duration;
        }
    }
    
    // Обновление списка замедленных врагов
    private void UpdateSlowedEnemies()
    {
        List<BaseCharacter> toRemove = new List<BaseCharacter>();
        
        foreach (var pair in slowedEnemies)
        {
            BaseCharacter enemy = pair.Key;
            float endTime = pair.Value;
            
            // Если время истекло или враг мертв
            if (Time.time >= endTime || enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    // Восстанавливаем скорость
                    enemy.moveSpeed /= (1f - slowPercentage);
                    Debug.Log($"{enemy.characterName} восстановил скорость");
                }
                
                toRemove.Add(enemy);
            }
        }
        
        // Удаляем из словаря
        foreach (var enemy in toRemove)
        {
            slowedEnemies.Remove(enemy);
        }
    }
    
    // Заморозка врага
    private IEnumerator FreezeEnemy(BaseCharacter enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy) yield break;
        
        // Сохраняем исходную скорость
        float originalSpeed = enemy.moveSpeed;
        float originalAttackSpeed = enemy.attackSpeed;
        
        // Полностью останавливаем противника
        enemy.moveSpeed = 0;
        enemy.attackSpeed = 0;
        
        Debug.Log($"{enemy.characterName} заморожен на {freezeDuration} секунд");
        
        // Визуальный эффект заморозки персонажа
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
        List<Material> originalMaterials = new List<Material>();
        
        foreach (Renderer renderer in renderers)
        {
            originalMaterials.Add(renderer.material);
            
            Material frozenMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            frozenMaterial.color = new Color(0.7f, 0.9f, 1f, 0.8f); // Ледяной цвет
            renderer.material = frozenMaterial;
        }
        
        // Ждем окончания эффекта
        yield return new WaitForSeconds(freezeDuration);
        
        // Восстанавливаем параметры, если персонаж все еще жив
        if (enemy != null && enemy.gameObject.activeInHierarchy)
        {
            enemy.moveSpeed = originalSpeed;
            enemy.attackSpeed = originalAttackSpeed;
            
            // Восстанавливаем оригинальные материалы
            renderers = enemy.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length && i < originalMaterials.Count; i++)
            {
                renderers[i].material = originalMaterials[i];
            }
            
            Debug.Log($"{enemy.characterName} разморожен");
        }
    }
    
    // Визуальный эффект области заморозки
    private IEnumerator FreezeAreaEffect(Vector3 position, float radius, float duration)
    {
        // Создаем визуальный эффект льда на земле
        GameObject iceEffect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        iceEffect.transform.position = new Vector3(position.x, 0.05f, position.z);
        iceEffect.transform.localScale = new Vector3(radius * 2, 0.1f, radius * 2);
        
        // Убираем коллайдер
        Destroy(iceEffect.GetComponent<Collider>());
        
        // Создаем ледяной материал
        Material iceMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        iceMaterial.color = new Color(0.8f, 0.95f, 1f, 0.6f); // Полупрозрачный лед
        iceEffect.GetComponent<Renderer>().material = iceMaterial;
        
        // Держим эффект на время заморозки
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            // Эффект пульсации
            float pulse = 1 + 0.05f * Mathf.Sin(elapsedTime * 5f);
            iceEffect.transform.localScale = new Vector3(
                radius * 2 * pulse,
                0.1f,
                radius * 2 * pulse
            );
            
            yield return null;
        }
        
        // Постепенно растворяем лед
        float fadeDuration = 0.5f;
        float fadeElapsed = 0;
        
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            
            // Уменьшаем прозрачность
            float alpha = 0.6f * (1 - fadeElapsed / fadeDuration);
            iceMaterial.color = new Color(0.8f, 0.95f, 1f, alpha);
            
            yield return null;
        }
        
        // Уничтожаем эффект
        Destroy(iceEffect);
    }
    
    // Визуальный эффект замедления
    private IEnumerator SlowVisualEffect(Transform enemyTransform, float duration)
    {
        if (enemyTransform == null) yield break;
        
        // Создаем эффект замедления
        GameObject slowEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        slowEffect.transform.position = enemyTransform.position;
        slowEffect.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        
        // Убираем коллайдер
        Destroy(slowEffect.GetComponent<Collider>());
        
        // Создаем материал эффекта
        Material slowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        slowMaterial.color = new Color(0, 0.5f, 0.9f, 0.3f); // Полупрозрачный синий
        slowEffect.GetComponent<Renderer>().material = slowMaterial;
        
        // Следуем за целью
        float elapsedTime = 0;
        while (elapsedTime < duration && enemyTransform != null)
        {
            elapsedTime += Time.deltaTime;
            
            // Обновляем позицию
            slowEffect.transform.position = enemyTransform.position;
            
            // Пульсирующий эффект
            float pulse = 0.6f + 0.1f * Mathf.Sin(elapsedTime * 3f);
            slowEffect.transform.localScale = new Vector3(pulse, pulse, pulse);
            
            yield return null;
        }
        
        // Уничтожаем эффект
        Destroy(slowEffect);
    }
    
    // Переопределяем анимацию атаки
    protected override IEnumerator AttackAnimation()
    {
        // Создаем временный объект для визуализации атаки магией
        GameObject magicOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        magicOrb.transform.position = transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;
        magicOrb.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        // Убираем коллайдер
        Destroy(magicOrb.GetComponent<Collider>());
        
        // Устанавливаем материал
        Material magicMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        magicMaterial.color = new Color(0, 0.7f, 1f, 0.7f); // Полупрозрачный синий
        magicOrb.GetComponent<Renderer>().material = magicMaterial;
        
        // Если есть цель, отправляем шар в ее сторону
        if (target != null)
        {
            float duration = 0.3f;
            float elapsed = 0;
            
            Vector3 startPos = magicOrb.transform.position;
            Vector3 targetPos = target.position + Vector3.up * 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                magicOrb.transform.position = Vector3.Lerp(startPos, targetPos, t);
                
                // Увеличиваем размер по мере движения
                float size = 0.2f + 0.2f * t;
                magicOrb.transform.localScale = new Vector3(size, size, size);
                
                yield return null;
            }
        }
        
        // Эффект удара
        if (target != null)
        {
            GameObject impactEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impactEffect.transform.position = target.position + Vector3.up * 0.5f;
            impactEffect.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            
            // Убираем коллайдер
            Destroy(impactEffect.GetComponent<Collider>());
            
            // Материал эффекта удара
            Material impactMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            impactMaterial.color = new Color(0, 0.7f, 1f, 0.5f); // Полупрозрачный синий
            impactEffect.GetComponent<Renderer>().material = impactMaterial;
            
            // Анимация затухания
            float impactDuration = 0.2f;
            float impactElapsed = 0;
            
            while (impactElapsed < impactDuration)
            {
                impactElapsed += Time.deltaTime;
                float t = impactElapsed / impactDuration;
                
                // Увеличиваем размер и уменьшаем прозрачность
                float size = 0.4f + 0.3f * t;
                impactEffect.transform.localScale = new Vector3(size, size, size);
                
                // Изменяем прозрачность
                float alpha = 0.5f * (1 - t);
                impactMaterial.color = new Color(0, 0.7f, 1f, alpha);
                
                yield return null;
            }
            
            // Уничтожаем эффект удара
            Destroy(impactEffect);
        }
        
        // Уничтожаем магический шар
        Destroy(magicOrb);
    }
}