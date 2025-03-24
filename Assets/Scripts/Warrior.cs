using UnityEngine;
using System.Collections;

public class Warrior : BaseCharacter
{
    // Специальные характеристики воина
    public float areaAttackRange = 3f;
    public float areaAttackCooldown = 5f;
    private float nextAreaAttackTime = 0f;
    
    // Инициализация
    protected override void Start()
    {
        base.Start();
        characterClass = CharacterClass.Warrior;
        characterName = "Воин";
        
        // Характеристики воина
        maxHealth = 150f;
        currentHealth = maxHealth;
        attackDamage = 15f;
        attackSpeed = 1.2f;
        attackRange = 2f;
        moveSpeed = 3f;
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
            
            // Если цель в пределах дальности атаки
            if (distanceToTarget <= attackRange)
            {
                // Атакуем
                if (CanAttack())
                {
                    Attack(target);
                }
                
                // Проверяем возможность использования особой атаки
                if (Time.time >= nextAreaAttackTime && currentEnergy >= 100f)
                {
                    AreaAttack();
                }
            }
            else
            {
                // Двигаемся к цели
                MoveTowards(target, attackRange);
            }
        }
    }
    
    // Особая атака по области
private void AreaAttack()
{
    nextAreaAttackTime = Time.time + areaAttackCooldown;
    currentEnergy -= 100f;
    
    Debug.Log($"{characterName} использует круговую атаку!");
    
    // Находим всех врагов в радиусе
    Collider[] hitColliders = Physics.OverlapSphere(transform.position, areaAttackRange);
    foreach (Collider collider in hitColliders)
    {
        // Сначала проверяем, есть ли компонент BaseCharacter
        BaseCharacter enemyCharacter = collider.GetComponent<BaseCharacter>();
        if (enemyCharacter != null && enemyCharacter.isEnemy && enemyCharacter != this)
        {
            enemyCharacter.TakeDamage(attackDamage * 0.7f);
            continue; // Переходим к следующему коллайдеру
        }
        
        // Для совместимости проверяем и EnemyController
        EnemyController enemy = collider.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(attackDamage * 0.7f);
        }
    }
    
    // Визуальный эффект атаки по области
    StartCoroutine(AreaAttackAnimation());
}
    
// Анимация круговой атаки
private IEnumerator AreaAttackAnimation()
{
    // Создаем временный объект для визуализации атаки
    GameObject areaEffect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    areaEffect.transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);
    areaEffect.transform.localScale = new Vector3(areaAttackRange * 2, 0.1f, areaAttackRange * 2);
    
    // Убираем коллайдер, нам нужен только визуальный эффект
    Destroy(areaEffect.GetComponent<Collider>());
    
    // Создаем и применяем материал
    Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    material.color = new Color(1, 0.5f, 0, 0.5f); // Полупрозрачный оранжевый
    areaEffect.GetComponent<Renderer>().material = material;
    
    // Анимируем расширение эффекта
    float duration = 0.5f;
    float elapsed = 0;
    
    Vector3 startScale = new Vector3(0.1f, 0.1f, 0.1f);
    Vector3 endScale = new Vector3(areaAttackRange * 2, 0.1f, areaAttackRange * 2);
    
    areaEffect.transform.localScale = startScale;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        
        // Плавно увеличиваем размер эффекта
        areaEffect.transform.localScale = Vector3.Lerp(startScale, endScale, t);
        
        // Постепенно уменьшаем непрозрачность
        material.color = new Color(1, 0.5f, 0, 0.5f * (1 - t) + 0.2f);
        
        yield return null;
    }
    
    // Ждем немного для наглядности
    yield return new WaitForSeconds(0.2f);
    
    // Удаляем эффект
    Destroy(areaEffect);
}

    // Анимация атаки по области
    protected override IEnumerator AttackAnimation()
    {
        // Создаем временный объект для визуализации атаки мечом
        GameObject swordEffect = GameObject.CreatePrimitive(PrimitiveType.Cube);
        swordEffect.transform.position = transform.position + transform.forward * 0.7f + Vector3.up * 0.5f;
        swordEffect.transform.localScale = new Vector3(0.1f, 0.8f, 0.2f);
        swordEffect.transform.rotation = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 0, 45);
        
        // Убираем коллайдер
        Destroy(swordEffect.GetComponent<Collider>());
        
        // Устанавливаем материал
        Material swordMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        swordMaterial.color = new Color(0.7f, 0.7f, 1f, 0.7f); // Синеватый стальной
        swordEffect.GetComponent<Renderer>().material = swordMaterial;
        
        // Анимируем взмах меча
        float duration = 0.3f;
        float elapsed = 0;
        
        Vector3 startPosition = swordEffect.transform.position;
        Quaternion startRotation = swordEffect.transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 0, 90);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            swordEffect.transform.rotation = Quaternion.Lerp(startRotation, endRotation, t);
            
            yield return null;
        }
        
        // Уничтожаем эффект
        Destroy(swordEffect);
    }
}