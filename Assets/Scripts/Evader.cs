using UnityEngine;
using System.Collections;

public class Evader : BaseCharacter
{
    // Специальные характеристики уклониста
    public float bounceDistance = 5f;    // Дистанция отскока
    public float poisonRadius = 3f;      // Радиус ядовитого облака
    public float poisonDuration = 3f;    // Длительность яда
    public float poisonDamage = 5f;      // Урон от яда в секунду
    public float poisonCooldown = 6f;    // Перезарядка отскока с ядом
    private float nextPoisonTime = 0f;
    public float triggerDistance = 2f;   // Дистанция для автоотскока
    
    // Инициализация
    protected override void Start()
    {
        base.Start();
        characterClass = CharacterClass.Evader;
        characterName = "Уклонист";
        
        // Характеристики уклониста
        maxHealth = 95f;
        currentHealth = maxHealth;
        attackDamage = 11f;
        attackSpeed = 0.9f;
        attackRange = 6f;
        moveSpeed = 3.3f;
        
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
        if (isDead) return;
        
        // Находим ближайшего врага
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            target = FindNearestTarget();
        }
        
        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            // Автоматический отскок, если враг слишком близко и способность готова
            if (distanceToTarget <= triggerDistance && Time.time >= nextPoisonTime && currentEnergy >= 100f)
            {
                PoisonBounce();
            }
            // Если цель в пределах дальности атаки
            else if (distanceToTarget <= attackRange)
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
                MoveTowards(target, attackRange * 0.7f);
            }
        }
    }
    
    // Отскок с созданием ядовитого облака
    private void PoisonBounce()
    {
        if (target == null) return;
        
        nextPoisonTime = Time.time + poisonCooldown;
        currentEnergy -= 100f;
        
        Debug.Log($"{characterName} выполняет отскок с ядовитым облаком!");
        
        // Запоминаем текущую позицию для создания яда
        Vector3 poisonPosition = transform.position;
        
        // Рассчитываем направление отскока (от цели)
        Vector3 bounceDirection = (transform.position - target.position).normalized;
        
        // Выполняем отскок
        StartCoroutine(BounceAnimation(transform.position + bounceDirection * bounceDistance));
        
        // Создаем ядовитое облако на прежней позиции
        StartCoroutine(PoisonCloudEffect(poisonPosition));
    }
    
    // Анимация отскока
    private IEnumerator BounceAnimation(Vector3 targetPosition)
    {
        float bounceDuration = 0.3f; // Быстрый отскок
        float elapsedTime = 0;
        Vector3 startPosition = transform.position;
        
        while (elapsedTime < bounceDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / bounceDuration;
            
            // Используем функцию синуса для дуги прыжка
            float heightFactor = Mathf.Sin(progress * Mathf.PI) * 1.5f;
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progress);
            currentPos.y = 0.5f + heightFactor; // Добавляем дугу прыжка
            
            transform.position = currentPos;
            
            yield return null;
        }
        
        // Убеждаемся, что персонаж приземлился в правильную позицию
        transform.position = new Vector3(targetPosition.x, 0.5f, targetPosition.z);
    }
    
    // Эффект ядовитого облака
    private IEnumerator PoisonCloudEffect(Vector3 position)
    {
        // Создаем облако яда
        GameObject poisonCloud = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        poisonCloud.transform.position = new Vector3(position.x, 0.1f, position.z);
        poisonCloud.transform.localScale = new Vector3(poisonRadius * 2, 0.2f, poisonRadius * 2);
        
        // Убираем коллайдер для визуального эффекта
        Destroy(poisonCloud.GetComponent<Collider>());
        
        // Создаем и применяем материал
        Material poisonMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        poisonMaterial.color = new Color(0.5f, 1f, 0.3f, 0.4f); // Зеленоватый полупрозрачный
        poisonCloud.GetComponent<Renderer>().material = poisonMaterial;
        
        // Список находящихся в облаке врагов для периодического нанесения урона
        Collider[] hitColliders = new Collider[10];
        float timePassed = 0f;
        float damageInterval = 0.5f; // Наносим урон каждые 0.5 секунды
        float nextDamageTime = 0f;
        
        // Анимируем и наносим урон, пока длится эффект
        while (timePassed < poisonDuration)
        {
            timePassed += Time.deltaTime;
            
            // Анимация расширения/сжатия облака
            float scaleMultiplier = 1f + 0.1f * Mathf.Sin(timePassed * 3f);
            poisonCloud.transform.localScale = new Vector3(
                poisonRadius * 2 * scaleMultiplier,
                0.2f,
                poisonRadius * 2 * scaleMultiplier
            );
            
            // Наносим периодический урон
            if (Time.time >= nextDamageTime)
            {
                nextDamageTime = Time.time + damageInterval;
                
                // Находим всех врагов в радиусе облака
                int hits = Physics.OverlapSphereNonAlloc(
                    poisonCloud.transform.position,
                    poisonRadius,
                    hitColliders
                );
                
                // Обрабатываем каждого врага
                for (int i = 0; i < hits; i++)
                {
                    BaseCharacter enemy = hitColliders[i].GetComponent<BaseCharacter>();
                    
                    // Проверяем, что это враг и он жив
                    if (enemy != null && enemy.isEnemy != isEnemy && enemy.currentHealth > 0)
                    {
                        // Наносим урон от яда за одну порцию (часть секундного урона)
                        float damage = poisonDamage * damageInterval;
                        enemy.TakeDamage(damage);
                        Debug.Log($"Яд наносит {damage} урона персонажу {enemy.characterName}");
                    }
                }
            }
            
            yield return null;
        }
        
        // Постепенно растворяем облако
        float fadeDuration = 0.5f;
        float fadeElapsed = 0f;
        
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float alpha = 0.4f * (1f - fadeElapsed / fadeDuration);
            poisonMaterial.color = new Color(0.5f, 1f, 0.3f, alpha);
            yield return null;
        }
        
        // Уничтожаем облако
        Destroy(poisonCloud);
    }
    
    // Переопределяем анимацию атаки
    protected override IEnumerator AttackAnimation()
    {
        // Создаем визуальный эффект броска дротика
        GameObject dart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dart.transform.position = transform.position + transform.forward * 0.5f + Vector3.up * 0.8f;
        dart.transform.localScale = new Vector3(0.05f, 0.3f, 0.05f);
        dart.transform.rotation = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(90, 0, 0);
        
        // Убираем коллайдер
        Destroy(dart.GetComponent<Collider>());
        
        // Устанавливаем материал
        Material dartMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        dartMaterial.color = new Color(0.3f, 0.8f, 0.3f); // Зеленоватый
        dart.GetComponent<Renderer>().material = dartMaterial;
        
        // Анимируем бросок дротика
        if (target != null)
        {
            float throwDuration = 0.3f;
            float elapsed = 0;
            
            Vector3 startPos = dart.transform.position;
            Vector3 targetPos = target.position + Vector3.up * 0.5f;
            
            while (elapsed < throwDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / throwDuration;
                
                // Движение по дуге
                Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
                pos.y += 1.0f * Mathf.Sin(t * Mathf.PI); // Дуга броска
                
                dart.transform.position = pos;
                dart.transform.forward = (targetPos - pos).normalized;
                
                yield return null;
            }
        }
        
        // Уничтожаем эффект
        Destroy(dart);
    }
}