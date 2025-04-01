using UnityEngine;
using System.Collections;

public class Tank : BaseCharacter
{
    // Специальные характеристики танка
    public float shieldDuration = 2f; // Длительность щита
    public float shieldCooldown = 10f; // Перезарядка щита
    private float nextShieldTime = 0f;
    private bool shieldActive = false;
    
    // Инициализация
    protected override void Start()
    {
        base.Start();
        characterClass = CharacterClass.Tank;
        characterName = "Танк";
        
        // Характеристики танка
        maxHealth = 150f;
        currentHealth = maxHealth;
        attackDamage = 15f;
        attackSpeed = 1.0f;
        attackRange = 2f;
        moveSpeed = 2.5f;
        
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
            
            // Проверяем возможность активации щита
            if (currentHealth < maxHealth * 0.5f && Time.time >= nextShieldTime && currentEnergy >= 100f)
            {
                ActivateShield();
            }
            
            // Если цель в пределах дальности атаки
            if (distanceToTarget <= attackRange)
            {
                // Атакуем
                if (CanAttack())
                {
                    Attack(target);
                }
            }
            else
            {
                // Двигаемся к цели
                MoveTowards(target, attackRange);
            }
        }
    }
    
    // Активация щита
    private void ActivateShield()
    {
        nextShieldTime = Time.time + shieldCooldown;
        currentEnergy -= 100f;
        shieldActive = true;
        
        Debug.Log($"{characterName} активирует защитный щит!");
        
        // Запускаем корутину для отображения и деактивации щита
        StartCoroutine(ShieldEffect());
    }
    
    // Переопределяем метод получения урона для работы щита
    public override void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // Если щит активен, превращаем урон в лечение
        if (shieldActive)
        {
            float healing = damage; // 100% конверсия урона в лечение
            currentHealth += healing;
            currentHealth = Mathf.Min(currentHealth, maxHealth); // Ограничиваем максимальным здоровьем
            
            Debug.Log($"{characterName} поглощает {damage} урона и восстанавливает {healing} здоровья!");
            
            // Визуальный эффект поглощения урона
            StartCoroutine(HealVisualEffect());
        }
        else
        {
            // Стандартное получение урона
            base.TakeDamage(damage);
        }
    }
    
    // Визуальный эффект щита
    private IEnumerator ShieldEffect()
    {
        // Создаем объект щита вокруг танка
        GameObject shieldObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldObject.transform.SetParent(transform);
        shieldObject.transform.localPosition = Vector3.zero;
        shieldObject.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
        
        // Убираем коллайдер
        Destroy(shieldObject.GetComponent<Collider>());
        
        // Настраиваем материал
        Material shieldMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        shieldMaterial.color = new Color(0, 0.5f, 1f, 0.3f); // Полупрозрачный синий
        shieldObject.GetComponent<Renderer>().material = shieldMaterial;
        
        // Ждем пока действует щит
        float elapsedTime = 0f;
        while (elapsedTime < shieldDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Пульсирующий эффект
            float scale = 1.8f + 0.2f * Mathf.Sin(elapsedTime * 5f);
            shieldObject.transform.localScale = new Vector3(scale, scale, scale);
            
            yield return null;
        }
        
        // Деактивируем щит
        shieldActive = false;
        Debug.Log($"{characterName}: Защитный щит деактивирован");
        
        // Уничтожаем визуальный эффект
        Destroy(shieldObject);
    }
    
    // Визуальный эффект лечения
    private IEnumerator HealVisualEffect()
    {
        // Создаем временный объект для визуализации эффекта
        GameObject healEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        healEffect.transform.position = transform.position + Vector3.up;
        healEffect.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        // Убираем коллайдер
        Destroy(healEffect.GetComponent<Collider>());
        
        // Устанавливаем материал
        Material healMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        healMaterial.color = new Color(0, 1, 0, 0.5f); // Полупрозрачный зеленый
        healEffect.GetComponent<Renderer>().material = healMaterial;
        
        // Анимируем эффект
        float duration = 0.5f;
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
    
    // Переопределяем анимацию атаки
    protected override IEnumerator AttackAnimation()
    {
        // Создаем временный объект для визуализации атаки щитом
        GameObject shieldBash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shieldBash.transform.position = transform.position + transform.forward * 0.7f + Vector3.up * 0.5f;
        shieldBash.transform.localScale = new Vector3(0.8f, 0.8f, 0.2f);
        
        // Убираем коллайдер
        Destroy(shieldBash.GetComponent<Collider>());
        
        // Устанавливаем материал
        Material shieldMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        shieldMaterial.color = new Color(0.4f, 0.4f, 0.7f, 0.8f); // Серо-синий
        shieldBash.GetComponent<Renderer>().material = shieldMaterial;
        
        // Анимируем удар щитом
        float duration = 0.3f;
        float elapsed = 0;
        
        Vector3 startPosition = shieldBash.transform.position;
        Vector3 endPosition = startPosition + transform.forward * 0.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (t < 0.5f)
            {
                // Движение вперед
                shieldBash.transform.position = Vector3.Lerp(startPosition, endPosition, t * 2);
            }
            else
            {
                // Движение назад
                shieldBash.transform.position = Vector3.Lerp(endPosition, startPosition, (t - 0.5f) * 2);
            }
            
            yield return null;
        }
        
        // Уничтожаем эффект
        Destroy(shieldBash);
    }
}
