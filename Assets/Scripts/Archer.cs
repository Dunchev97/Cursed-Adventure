using UnityEngine;
using System.Collections;

public class Archer : BaseCharacter
{
    // Специальные характеристики стрелка
    public float safeDistance = 5f; // Дистанция, на которой стрелок хочет оставаться
    public GameObject arrowPrefab; // Префаб стрелы
    public float powerShotCooldown = 8f;
    private float nextPowerShotTime = 0f;
    
    // Инициализация
    protected override void Start()
    {
        base.Start();
        characterClass = CharacterClass.Archer;
        characterName = "Стрелок";
        
        // Характеристики стрелка
        maxHealth = 90f;
        currentHealth = maxHealth;
        attackDamage = 12f;
        attackSpeed = 0.8f;
        attackRange = 8f; // Большая дальность атаки
        moveSpeed = 3.5f; // Немного быстрее воина
        
        // Создаем простой префаб стрелы, если не назначен
        if (arrowPrefab == null)
        {
            arrowPrefab = CreateArrowPrefab();
        }
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
            
            // Если враг слишком близко, отходим и не атакуем
            if (distanceToTarget < safeDistance)
            {
                Vector3 directionAway = (transform.position - target.position).normalized;
                transform.position += directionAway * moveSpeed * Time.deltaTime;
                transform.forward = -directionAway; // Продолжаем смотреть на врага
            }
            // Если враг в пределах дальности атаки
            // Если враг в оптимальной дистанции атаки - останавливаемся и атакуем
            else if (distanceToTarget <= attackRange && distanceToTarget >= safeDistance)
            {
                // Останавливаемся и атакуем
                transform.forward = (target.position - transform.position).normalized;
                
                // Проверка, что лучник не в движении
                bool isStationary = true;
                
                if (isStationary)
                {
                    if (CanAttack())
                    {
                        Attack(target);
                    }
                    
                    // Проверяем возможность использования мощного выстрела
                    if (Time.time >= nextPowerShotTime && currentEnergy >= 100f) // Изменено энергопотребление
                    {
                        PowerShot();
                    }
                }
            }
            else
            {
                // Двигаемся к цели до оптимальной дистанции
                MoveTowards(target, safeDistance);
            }
        }
    }
    
    // Переопределяем метод атаки для стрелка
    protected override void Attack(Transform attackTarget)
    {
        if (attackTarget == null || !CanAttack()) return;
        
        // Устанавливаем время следующей атаки
        nextAttackTime = Time.time + (1f / attackSpeed);
        
        // Запускаем стрелу
        StartCoroutine(ShootArrow(attackTarget, attackDamage, false));
    }
    
    // Мощный выстрел (особая способность)
    private void PowerShot()
    {
        if (target == null) return;
        
        nextPowerShotTime = Time.time + powerShotCooldown;
        currentEnergy -= 100f;
        
        Debug.Log($"{characterName} использует мощный выстрел!");
        
        // Запускаем мощную стрелу
        StartCoroutine(ShootArrow(target, attackDamage * 2.5f, true));
    }
    
    // Корутина для запуска стрелы
    private IEnumerator ShootArrow(Transform target, float damage, bool isPowerShot)
    {
        // Создаем стрелу
        GameObject arrow = Instantiate(arrowPrefab, 
            transform.position + transform.forward * 0.5f + Vector3.up * 0.5f, 
            Quaternion.identity);
        
        // Если это мощный выстрел, делаем стрелу больше и другого цвета
        if (isPowerShot)
        {
            arrow.transform.localScale *= 1.5f;
            Renderer arrowRenderer = arrow.GetComponent<Renderer>();
            if (arrowRenderer != null)
            {
                arrowRenderer.material.color = Color.red;
            }
        }
        
        // Запускаем стрелу в направлении цели
        float arrowSpeed = 20f;
        float arrowLifetime = 2f;
        float elapsedTime = 0;
        
        Vector3 startPosition = arrow.transform.position;
        Vector3 targetPosition = target.position + Vector3.up * 0.5f;
        
        while (elapsedTime < arrowLifetime)
        {
            elapsedTime += Time.deltaTime;
            float journeyFraction = elapsedTime * arrowSpeed / Vector3.Distance(startPosition, targetPosition);
            
            if (journeyFraction >= 1f)
            {
                // Стрела достигла цели
                if (target != null)
                {
                    // Наносим урон
                    BaseCharacter targetCharacter = target.GetComponent<BaseCharacter>();
                    if (targetCharacter != null)
                    {
                        targetCharacter.TakeDamage(damage);
                    }
                    else
                    {
                        EnemyController targetEnemy = target.GetComponent<EnemyController>();
                        if (targetEnemy != null)
                        {
                            targetEnemy.TakeDamage(damage);
                        }
                    }
                }
                
                break;
            }
            
            // Обновляем позицию стрелы
            if (target != null && target.gameObject.activeInHierarchy)
            {
                // Если цель еще жива, обновляем её позицию
                targetPosition = target.position + Vector3.up * 0.5f;
            }
            
            // Движение стрелы
            arrow.transform.position = Vector3.Lerp(startPosition, targetPosition, journeyFraction);
            
            // Направление стрелы
            if ((targetPosition - arrow.transform.position).sqrMagnitude > 0.01f)
            {
                arrow.transform.forward = (targetPosition - arrow.transform.position).normalized;
            }
            
            yield return null;
        }
        
        // Уничтожаем стрелу
        Destroy(arrow);
    }
    
    // Создаем простой префаб стрелы
    private GameObject CreateArrowPrefab()
    {
        GameObject arrow = new GameObject("Arrow");
        
        // Добавляем визуальный компонент
        GameObject arrowMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrowMesh.transform.SetParent(arrow.transform);
        arrowMesh.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
        arrowMesh.transform.localPosition = Vector3.zero;
        arrowMesh.transform.Rotate(90, 0, 0);
        
        // Создаем наконечник
        GameObject arrowTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
// Деформируем сферу, чтобы она напоминала наконечник стрелы
arrowTip.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);
        arrowTip.transform.SetParent(arrow.transform);
        arrowTip.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);
        arrowTip.transform.localPosition = new Vector3(0, 0, 0.5f);
        arrowTip.transform.Rotate(90, 0, 0);
        
        // Убираем коллайдеры
        Destroy(arrowMesh.GetComponent<Collider>());
        Destroy(arrowTip.GetComponent<Collider>());
        
        // Устанавливаем материал
        Material arrowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        arrowMaterial.color = Color.yellow;
        arrowMesh.GetComponent<Renderer>().material = arrowMaterial;
        arrowTip.GetComponent<Renderer>().material = arrowMaterial;
        
        return arrow;
    }
}