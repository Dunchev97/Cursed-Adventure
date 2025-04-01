using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CharacterState = BaseCharacter.CharacterState;

public class BattleManager : MonoBehaviour
{
    // Флаги для контроля вывода логов
    private bool enableDebugLogs = false; // Обычные отладочные сообщения
    private bool enableWarningLogs = true; // Предупреждения
    private bool enableErrorLogs = true; // Ошибки
    private bool enableImportantLogs = true; // Важные события

    private TMPro.TextMeshProUGUI tmpTextComponent;
    
    public static bool IsFightStarted = false;

    // Ссылки на префабы персонажей
    public GameObject warriorPrefab;
    public GameObject archerPrefab;
    public GameObject supportPrefab;
    public GameObject assassinPrefab;
    public GameObject tankPrefab;
    public GameObject evaderPrefab;
    public GameObject specialistPrefab;

    public UnityEngine.UI.Button startBattleButton; // В начале класса

    public GameObject placementIndicatorPrefab;
    private GameObject currentPlacementIndicator;
    
    private bool battleStarted = false;

    // Добавьте после префабов персонажей игрока
    public GameObject enemyWarriorPrefab;
    public GameObject enemyArcherPrefab;
    public GameObject enemySupportPrefab;
    public GameObject enemyAssassinPrefab;
    public GameObject enemyTankPrefab;
    public GameObject enemyEvaderPrefab;
    public GameObject enemySpecialistPrefab;

    public GameObject placementInstructionTextObject;
    private UnityEngine.UI.Text placementInstructionText;
    
    // Ссылка на префаб врага
    public GameObject enemyPrefab;
    
    // Настройки арены
    public Transform playerSpawnArea;
    public Transform enemySpawnArea;
    
    // Состояние боя
    private enum BattleState { Setup, PlayerTurn, EnemyTurn, Battle, Victory, Defeat }
    private BattleState currentState;
    
    // Списки персонажей
    private List<BaseCharacter> playerCharacters = new List<BaseCharacter>();
    private List<BaseCharacter> enemies = new List<BaseCharacter>();
    
    // Количество противников
    public int numberOfEnemies = 3;
    // Кнопки для выбора сложности
    public UnityEngine.UI.Button easyDifficultyButton;
    public UnityEngine.UI.Button mediumDifficultyButton;
    public UnityEngine.UI.Button hardDifficultyButton;

    // Фаза выбора сложности
    private bool difficultySelectionPhase = true;
    // Этап выбора персонажей
    private bool characterSelectionPhase = true;

    // Панель выбора персонажей
    public GameObject characterSelectionPanel;

    // Список выбранных персонажей
    private List<string> selectedCharacters = new List<string>();
    
    // Временные флаги для размещения персонажей
    private int charactersPlaced = 0;
    private bool isPlacingCharacters = false;
    private GameObject currentCharacterToPlace;
    
    // Префаб полоски здоровья
    public GameObject healthBarPrefab;
    
    // Ссылка на экземпляр для синглтона
    public static BattleManager Instance { get; private set; }

    // Метод Awake вызывается при создании объекта
    void Awake()
    {
        // Реализация синглтона
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void OnDrawGizmos()
{
    if (playerSpawnArea != null)
    {
        Gizmos.color = new Color(0, 0, 1, 0.3f);
        Gizmos.DrawCube(playerSpawnArea.position, playerSpawnArea.localScale);
    }
}
    
    // Метод Start вызывается перед первым кадром
    void Start()
    {
        // Находим TextMeshPro компонент, если он есть
        tmpTextComponent = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        
        // Находим компонент Text для инструкций размещения
        if (placementInstructionTextObject != null)
        {
            placementInstructionText = placementInstructionTextObject.GetComponent<UnityEngine.UI.Text>();
        }
        
        // Устанавливаем начальное состояние
        currentState = BattleState.Setup;
        
        // Создаем префабы персонажей, если они не назначены
        CreateDefaultPrefabsIfNeeded();
        
        // Сначала показываем выбор персонажей
        characterSelectionPhase = true;
        difficultySelectionPhase = false;
        
        // Показываем панель выбора персонажей
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(true);
        }
        else
        {
            LogError("Панель выбора персонажей не назначена!");
            // Если панель не назначена, переходим сразу к выбору сложности
            difficultySelectionPhase = true;
            SetupDifficultyButtons();
        }
        
        // Настройка тегов для правильной работы логики
        gameObject.tag = "BattleManager";
        
        // Визуализация зон спавна для отладки
        if (playerSpawnArea != null)
        {
            GameObject playerAreaMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerAreaMarker.name = "PlayerSpawnAreaMarker";
            playerAreaMarker.transform.position = playerSpawnArea.position;
            playerAreaMarker.transform.localScale = new Vector3(1, 0.1f, 1);
            playerAreaMarker.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 0.3f);
            Destroy(playerAreaMarker.GetComponent<Collider>());
        }
        
        if (enemySpawnArea != null)
        {
            GameObject enemyAreaMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyAreaMarker.name = "EnemySpawnAreaMarker";
            enemyAreaMarker.transform.position = enemySpawnArea.position;
            enemyAreaMarker.transform.localScale = new Vector3(1, 0.1f, 1);
            enemyAreaMarker.GetComponent<Renderer>().material.color = new Color(1, 0, 0, 0.3f);
            Destroy(enemyAreaMarker.GetComponent<Collider>());
        }
        
        // Отключаем кнопку начала боя в начале
        if (startBattleButton != null)
        {
            startBattleButton.gameObject.SetActive(false);
        }
        
        LogDebug($"BattleManager инициализирован. Time.timeScale = {Time.timeScale}");
    }

    public bool IsBattleStarted()
    {
        return battleStarted;
    }

    public void OnStartBattleButtonClick()
    {
        LogDebug("Кнопка 'Начать бой' нажата! Персонажей размещено: " + charactersPlaced);
        
        // Проверяем, все ли персонажи размещены
        if (charactersPlaced >= playerCharacters.Count)
        {
            LogDebug("Запускаем бой");
            // Запускаем бой
            StartBattle();
            
            // Скрываем кнопку
            if (startBattleButton != null)
            {
                startBattleButton.gameObject.SetActive(false);
            }
        }
        else
        {
            LogWarning("Не все персонажи размещены! Размещено: " + charactersPlaced + " из " + playerCharacters.Count);
        }
    }
    
 // Обновление каждый кадр
 void Update()
{
    DebugObjectsState();
    
    // Если мы размещаем персонажей
    if (isPlacingCharacters && currentCharacterToPlace != null)
    {
        // Перемещаем персонажа к курсору каждый кадр
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        float distance;
        
        if (ground.Raycast(ray, out distance))
        {
            Vector3 position = ray.GetPoint(distance);
            position.y = 0.5f; // Фиксируем высоту
            
            // Проверяем, можно ли разместить персонажа здесь
           bool positionIsClear = true;
            Collider[] colliders = Physics.OverlapSphere(position, 0.7f);

            // Проверяем, нет ли рядом врагов или других персонажей игрока
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("Enemy"))
                {
                    positionIsClear = false;
                    break;
                }
                // Проверяем близость к другим персонажам игрока (чтобы не размещать на одном месте)
                BaseCharacter otherChar = col.GetComponent<BaseCharacter>();
                if (otherChar != null && !otherChar.isEnemy && otherChar.gameObject != currentCharacterToPlace)
                {
                    // Если это другой персонаж игрока, не разрешаем размещение
                    positionIsClear = false;
                    break;
                }
            }
            
            // Обновляем индикатор размещения
            if (currentPlacementIndicator != null)
            {
                currentPlacementIndicator.transform.position = position;
                Renderer markerRenderer = currentPlacementIndicator.GetComponent<Renderer>();
                if (markerRenderer != null)
                {
                    markerRenderer.material.color = positionIsClear 
                        ? new Color(0, 1, 0, 0.3f)  // Зелёный - можно разместить
                        : new Color(1, 0, 0, 0.3f); // Красный - нельзя разместить
                }
            }
            
            // Перемещаем персонажа за курсором
            currentCharacterToPlace.transform.position = position;
            
            // При клике размещаем персонажа
            if (Input.GetMouseButtonDown(0) && positionIsClear)
            {
                LogDebug($"Персонаж {currentCharacterToPlace.name} размещен в позиции {position}");
                
                // Увеличиваем счетчик размещенных персонажей
                charactersPlaced++;
                LogDebug($"Размещено персонажей: {charactersPlaced} из {playerCharacters.Count}");
                
                // Удаляем индикатор размещения
                if (currentPlacementIndicator != null)
                {
                    Destroy(currentPlacementIndicator);
                    currentPlacementIndicator = null;
                }
                
                // Проверяем, все ли персонажи размещены
                if (charactersPlaced >= playerCharacters.Count)
                {
                    isPlacingCharacters = false;
                    if (startBattleButton != null)
                    {
                        startBattleButton.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // Готовим следующего персонажа для размещения
                    currentCharacterToPlace = null;
                    ShowNextCharacterToPlace();
                }
            }
        }
    }
    
    // Если бой начался, проверяем его состояние
    if (battleStarted)
    {
        CheckBattleState();
    }
}

    // Настройка боя
    IEnumerator SetupBattle()
    {
        LogDebug("Настройка боя...");
        
        
        // Даем игроку возможность разместить персонажей
        yield return StartCoroutine(PlacePlayerCharacters());
    }
    
    // Создание врагов
void SpawnEnemies()
{
    LogDebug("Создание противников...");
    
    // Находим объект арены
    GameObject arena = GameObject.FindGameObjectWithTag("Arena");
    
    // Если не нашли объект с тегом "Arena", пробуем найти по имени
    if (arena == null)
    {
        arena = GameObject.Find("BattleArena");
    }
    
    // Если всё еще не нашли, используем объект enemySpawnArea для определения центра арены
    Vector3 arenaCenter = arena != null ? arena.transform.position : enemySpawnArea.position;
    float arenaRadius = 5f; // Примерный радиус арены, настрой под свою сцену
    
    // Создаем противников в стратегически важных позициях
    Vector3[] enemyPositions = new Vector3[numberOfEnemies];
    
    // Распределяем противников по кругу
    for (int i = 0; i < numberOfEnemies; i++)
    {
        float angle = (360f / numberOfEnemies) * i * Mathf.Deg2Rad; // Преобразуем градусы в радианы
        float distanceFromCenter = arenaRadius * 0.6f; // Примерно на 60% от края к центру
        
        float x = arenaCenter.x + Mathf.Cos(angle) * distanceFromCenter;
        float z = arenaCenter.z + Mathf.Sin(angle) * distanceFromCenter;
        
        enemyPositions[i] = new Vector3(x, 0.5f, z);
    }
    
    // Создаем разных типов врагов для разнообразия
    for (int i = 0; i < numberOfEnemies; i++)
    {
        // Случайный выбор типа противника
        int enemyType = Random.Range(0, 7); // Теперь чередуем 7 типов врагов// Чередуем 4 типа врагов
        GameObject enemyPrefab = null;
        
        switch (enemyType)
        {
            case 0:
                enemyPrefab = enemyWarriorPrefab;
                break;
            case 1:
                enemyPrefab = enemyArcherPrefab;
                break;
            case 2:
                enemyPrefab = enemySupportPrefab;
                break;
            case 3:
                enemyPrefab = enemyAssassinPrefab;
                break;
            case 4:
                enemyPrefab = enemyTankPrefab;
                break;
            case 5:
                enemyPrefab = enemyEvaderPrefab;
                break;
            case 6:
                enemyPrefab = enemySpecialistPrefab;
                break;
        }
        
        // Создаем врага в заранее рассчитанной позиции
        if (enemyPrefab != null)
        {
            Vector3 position = enemyPositions[i];
            
            GameObject enemyObject = Instantiate(enemyPrefab, position, Quaternion.identity);
            enemyObject.name = "Enemy " + (i + 1);
            enemyObject.tag = "Enemy"; // Используем стандартный тег "Enemy"
            
            // Получаем компонент BaseCharacter и настраиваем
            BaseCharacter enemyCharacter = enemyObject.GetComponent<BaseCharacter>();
            if (enemyCharacter != null)
            {
                enemyCharacter.isEnemy = true;     // Это враг
                enemyCharacter.isBattleReady = false; // Не готов к бою
                enemyCharacter.currentState = CharacterState.Idle; // Явно устанавливаем состояние ожидания
                enemies.Add(enemyCharacter);
            }
            
            /// Гарантированно отключаем скрипт EnemyController
EnemyController controller = enemyObject.GetComponent<EnemyController>();
if (controller != null)
{
    LogDebug($"Отключаем EnemyController для {enemyObject.name}");
    controller.enabled = false;
}
else
{
    LogError($"Не найден EnemyController на {enemyObject.name}! Добавляем...");
    // Если его нет, добавляем и сразу отключаем
    controller = enemyObject.AddComponent<EnemyController>();
    controller.enabled = false;
}
            
            // Добавляем визуальные элементы - для отображения информации игроку
            AddTeamMarker(enemyObject, true);
            AddHealthBar(enemyObject);
            
            // Добавляем мягкий визуальный эффект "не готов к бою"
            Renderer[] renderers = enemyObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                // Не делаем полностью прозрачным, чтобы игрок видел
                Material mat = r.material;
                Color color = mat.color;
                // Слегка тонируем, чтобы показать, что враг не активен
                color = new Color(color.r, color.g, color.b, 0.7f);
                mat.color = color;
            }
            
            LogDebug($"Создан противник {enemyObject.name} в позиции {position}");
        }
    }
    
    LogDebug($"Всего создано {enemies.Count} противников");
}

    void AddHealthBar(GameObject character)
    {
        if (healthBarPrefab == null)
        {
            LogWarning("Префаб полоски здоровья не назначен!");
            return;
        }
        
        // Создаем полоску здоровья и прикрепляем к персонажу
        GameObject healthBar = Instantiate(healthBarPrefab, character.transform);
        healthBar.transform.localPosition = new Vector3(0, 2f, 0); // Размещаем над головой
        
        // Настраиваем полоску здоровья
        // Предполагаем, что у нас есть компонент для отображения здоровья
        // Если компонент называется по-другому, замените "HealthBar" на правильное имя
        MonoBehaviour healthBarComponent = healthBar.GetComponent<MonoBehaviour>();
        if (healthBarComponent != null)
        {
            BaseCharacter baseCharacter = character.GetComponent<BaseCharacter>();
            if (baseCharacter != null)
            {
                // Вместо вызова SetupHealthBar, который может отсутствовать,
                // устанавливаем родителя для полоски здоровья
                healthBar.transform.SetParent(character.transform);
                LogDebug($"Добавлена полоска здоровья к персонажу {character.name}");
            }
        }
    }

    void AddTeamMarker(GameObject character, bool isEnemy)
    {
        // Создаем маркер
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.transform.SetParent(character.transform);
        marker.transform.localPosition = new Vector3(0, 1.5f, 0);
        marker.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        
        // Убираем коллайдер
        Destroy(marker.GetComponent<Collider>());
        
        // Устанавливаем цвет
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = isEnemy ? Color.red : Color.blue;
        marker.GetComponent<Renderer>().material = material;
    }

    // Размещение персонажей игрока
    IEnumerator PlacePlayerCharacters()
    {
        LogDebug("Размещение персонажей игрока...");
        
        // Создаем персонажей игрока
        CreatePlayerCharacters();
        
        // Устанавливаем флаг размещения персонажей
        isPlacingCharacters = true;
        charactersPlaced = 0;
        
        // Показываем первого персонажа для размещения
        ShowNextCharacterToPlace();
        
        // Ждем, пока все персонажи будут размещены
        // yield return new WaitUntil(() => !isPlacingCharacters);
        
        // Вместо этого используем другой подход
        while (isPlacingCharacters && charactersPlaced < playerCharacters.Count)
        {
            yield return null; // Ждем один кадр и проверяем снова
        }
        
        LogDebug("Все персонажи размещены!");
    }
    
    // Автоматическое размещение персонажей игрока (для тестирования)
    void AutoPlacePlayerCharacters()
    {
        LogDebug("Автоматическое размещение персонажей игрока...");
        
        // Создаем персонажей игрока
        CreatePlayerCharacters();
        
        // Размещаем персонажей автоматически
        Vector3 basePosition = playerSpawnArea.position;
        float spacing = 1.5f;
        
        for (int i = 0; i < playerCharacters.Count; i++)
        {
            GameObject characterObject = playerCharacters[i].gameObject;
            characterObject.SetActive(true);
            
            // Размещаем персонажей в линию с небольшим смещением
            Vector3 position = basePosition + new Vector3(i * spacing - (playerCharacters.Count - 1) * spacing / 2, 0.5f, 0);
            characterObject.transform.position = position;
            
            // Увеличиваем счетчик размещенных персонажей
            charactersPlaced++;
        }
        
        // Все персонажи размещены
        isPlacingCharacters = false;
        
        // Активируем кнопку начала боя
        if (startBattleButton != null)
        {
            startBattleButton.gameObject.SetActive(true);
        }
        
        LogDebug("Все персонажи автоматически размещены!");
    }
    
    // Показать следующего персонажа для размещения
    void ShowNextCharacterToPlace()
    {

        int index = charactersPlaced;
        if (index < playerCharacters.Count)
        {
            GameObject characterObject = playerCharacters[index].gameObject;
            characterObject.SetActive(true);
            
            // Устанавливаем начальную позицию для размещения
            characterObject.transform.position = new Vector3(
                playerSpawnArea.position.x,
                0.5f,
                playerSpawnArea.position.z
            );
            
            currentCharacterToPlace = characterObject;
            
            // Создаем простой индикатор размещения
            if (currentPlacementIndicator == null)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "PlacementMarker";
                marker.transform.position = characterObject.transform.position;
                marker.transform.localScale = new Vector3(1.0f, 0.05f, 1.0f);
                // Удаляем коллайдер
                Destroy(marker.GetComponent<Collider>());
                // Делаем полупрозрачным
                Renderer markerRenderer = marker.GetComponent<Renderer>();
                Material markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                markerMaterial.color = new Color(0, 1, 0, 0.3f); // Полупрозрачный зеленый
                markerRenderer.material = markerMaterial;
                // Запоминаем для последующего удаления
                currentPlacementIndicator = marker;
            }

            // Обновляем текст подсказки в зависимости от типа персонажа
            string instructionText = $"Разместите {characterObject.name} на поле боя.";
            
            // Отладочное сообщение с текстом инструкции
            LogImportant("ТЕКСТ ИНСТРУКЦИИ: " + instructionText);

            // Обновляем текст в UI используя доступные компоненты
            if (tmpTextComponent != null)
            {
                tmpTextComponent.text = instructionText;
                tmpTextComponent.gameObject.SetActive(true);
            }
            else if (placementInstructionText != null)
            {
                placementInstructionText.text = instructionText;
                placementInstructionText.gameObject.SetActive(true);
            }
        }
        
        // Проверяем, все ли персонажи размещены после размещения текущего
        if (charactersPlaced >= playerCharacters.Count)
        {
            LogDebug("Все персонажи размещены. Подготовка к активации кнопки начала боя.");
            
            // Активируем кнопку начала боя после размещения всех персонажей
            if (startBattleButton != null)
            {
                startBattleButton.gameObject.SetActive(true);
                LogDebug("Кнопка 'Начать бой' активирована.");
            }
        }
        
    }
    
    // Показ инструкций по размещению персонажей
    void ShowPlacementInstructions(bool show)
    {
        // Здесь можно добавить код для отображения UI-инструкций
        // Например, текст "Кликните, чтобы разместить воина" и т.д.
        if (show)
        {
            LogDebug("Инструкции: Кликните левой кнопкой мыши на арене, чтобы разместить персонажа");
        }
    }
    
    // Начало боя
// Начало боя
void StartBattle()
{
    LogImportant("=== НАЧАЛО БОЯ ===");
    
    // Устанавливаем глобальный флаг начала боя
    BattleManager.IsFightStarted = true;
    currentState = BattleState.Battle;
    battleStarted = true;

    // Замедляем время боя
    Time.timeScale = 0.5f;
    // Скрываем кнопку начала боя
if (startBattleButton != null)
{
    startBattleButton.gameObject.SetActive(false);
}

// Показываем текст "Идёт бой!" в начале боя
if (tmpTextComponent != null)
{
    tmpTextComponent.text = "Идёт бой!";
    tmpTextComponent.fontSize = 28; // Увеличиваем размер текста
    tmpTextComponent.color = Color.yellow; // Желтый для обозначения активного боя
    tmpTextComponent.gameObject.SetActive(true);
}
else if (placementInstructionText != null)
{
    placementInstructionText.text = "Идёт бой!";
    placementInstructionText.fontSize = 28; // Увеличиваем размер текста
    placementInstructionText.color = Color.yellow; // Желтый для обозначения активного боя
    
    placementInstructionText.gameObject.SetActive(true);
}
    
    // Перевод врагов в состояние готовности
    foreach (var enemy in enemies)
    {
        if (enemy != null)
        {
            LogDebug($"Активация врага {enemy.gameObject.name}...");
            
            // Восстанавливаем полную видимость
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                Material mat = r.material;
                Color color = mat.color;
                color = new Color(color.r, color.g, color.b, 1.0f);
                mat.color = color;
            }
            
            // Устанавливаем состояние готовности (НЕ боевое состояние)
            enemy.currentState = CharacterState.Ready;
            
            // Флаг для обратной совместимости
            enemy.isBattleReady = true;
        }
    }
    
    // Перевод игроков в состояние готовности
    foreach (var playerChar in playerCharacters)
    {
        if (playerChar != null && playerChar.gameObject.activeInHierarchy)
        {
            playerChar.currentState = CharacterState.Ready;
        }
    }
    
    
    // Сначала включим статус Ready для всех,
    // затем с задержкой переведём их в боевой режим
    StartCoroutine(DelayedCombatStart(1.0f));
    
    LogDebug($"Количество противников: {enemies.Count}");
    LogDebug($"Количество персонажей игрока: {playerCharacters.Count}");
    LogDebug("Подготовка к бою завершена!");
}

// Метод для отложенного начала боевых действий
private IEnumerator DelayedCombatStart(float delay)
{
    yield return new WaitForSeconds(delay);
    
    LogImportant("!!! БОЙ НАЧИНАЕТСЯ !!!");
    
    // Активируем боевой режим у всех персонажей
    foreach (var enemy in enemies)
    {
        if (enemy != null && enemy.gameObject.activeInHierarchy)
        {
            enemy.currentState = CharacterState.Combat;
        }
    }
    
    foreach (var playerChar in playerCharacters)
    {
        if (playerChar != null && playerChar.gameObject.activeInHierarchy)
        {
            playerChar.currentState = CharacterState.Combat;
        }
    }
}
// Скрываем текст через указанное время
private IEnumerator HideTextAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    
    if (placementInstructionText != null)
    {
        placementInstructionText.gameObject.SetActive(false);
    }
}
    
    // Победа
public void Victory()
{
    // Возвращаем нормальную скорость времени
    Time.timeScale = 1.0f;
    if (currentState == BattleState.Victory) return; // Предотвращаем повторный вызов
    
    currentState = BattleState.Victory;
    LogImportant("!!! ПОБЕДА !!! Все враги повержены.");
    
    // Создаем UI сообщение о победе
    // Создаем UI сообщение о победе
if (tmpTextComponent != null)
{
    tmpTextComponent.text = "ПОБЕДА!";
    tmpTextComponent.fontSize = 36;
    tmpTextComponent.color = Color.green;
    tmpTextComponent.gameObject.SetActive(true);
    
    // Добавляем небольшую анимацию или эффект при победе
    StartCoroutine(VictoryTextAnimation(tmpTextComponent));
}
else if (placementInstructionText != null)
{
    placementInstructionText.text = "ПОБЕДА!";
    placementInstructionText.fontSize = 36;
    placementInstructionText.color = Color.green;
    placementInstructionText.gameObject.SetActive(true);
}
}

// Поражение
public void Defeat()
{
     // Возвращаем нормальную скорость времени
    Time.timeScale = 1.0f;
    if (currentState == BattleState.Defeat) return; // Предотвращаем повторный вызов
    
    currentState = BattleState.Defeat;
    LogImportant("!!! ПОРАЖЕНИЕ !!! Все персонажи игрока погибли.");
    
    // Создаем UI сообщение о поражении
if (tmpTextComponent != null)
{
    tmpTextComponent.text = "ПОРАЖЕНИЕ";
    tmpTextComponent.fontSize = 36;
    tmpTextComponent.color = Color.red;
    tmpTextComponent.gameObject.SetActive(true);
}
else if (placementInstructionText != null)
{
    placementInstructionText.text = "ПОРАЖЕНИЕ";
    placementInstructionText.fontSize = 36;
    placementInstructionText.color = Color.red;
    placementInstructionText.gameObject.SetActive(true);
}
}
    
    // Создание стандартных префабов, если они не назначены
    void CreateDefaultPrefabsIfNeeded()
    {
        // Создаем префаб воина
        if (warriorPrefab == null)
        {
            warriorPrefab = CreateDefaultCharacterPrefab("Warrior", Color.blue);
        }
        
        // Создаем префаб стрелка
        if (archerPrefab == null)
        {
            archerPrefab = CreateDefaultCharacterPrefab("Archer", Color.green);
        }
        
        // Создаем префаб персонажа поддержки
        if (supportPrefab == null)
        {
            supportPrefab = CreateDefaultCharacterPrefab("Support", Color.yellow);
        }
        
        // Создаем префаб убийцы
        if (assassinPrefab == null)
        {
            assassinPrefab = CreateDefaultCharacterPrefab("Assassin", Color.magenta);
        }
        
        // Создаем префаб танка
        if (tankPrefab == null)
        {
            tankPrefab = CreateDefaultCharacterPrefab("Tank", Color.cyan);
        }
        
        // Создаем префаб уклониста
        if (evaderPrefab == null)
        {
            evaderPrefab = CreateDefaultCharacterPrefab("Evader", Color.grey);
        }
        
        // Создаем префаб специалиста
        if (specialistPrefab == null)
        {
            specialistPrefab = CreateDefaultCharacterPrefab("Specialist", Color.white);
        }
        
        // Создаем префаб врага
        if (enemyPrefab == null)
        {
            enemyPrefab = CreateDefaultCharacterPrefab("Enemy", Color.red);
            
            // Добавляем компонент EnemyController
            enemyPrefab.AddComponent<EnemyController>();
            
            // Отключаем префаб, чтобы он не был виден в сцене
            enemyPrefab.SetActive(false);
        }
    }
    
    // Создание стандартного префаба персонажа
    GameObject CreateDefaultCharacterPrefab(string characterType, Color color)
    {
        GameObject prefab = new GameObject(characterType + "Prefab");
        
        // Добавляем примитив для визуального представления
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.transform.SetParent(prefab.transform);
        visual.transform.localPosition = Vector3.zero;
        
        // Устанавливаем материал
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        visual.GetComponent<Renderer>().material = material;
        
        // Добавляем компоненты
        prefab.AddComponent<CapsuleCollider>();
        
        // Добавляем соответствующий скрипт
        switch (characterType)
        {
            case "Warrior":
                prefab.AddComponent<Warrior>();
                break;
            case "Archer":
                prefab.AddComponent<Archer>();
                break;
            case "Support":
                prefab.AddComponent<Support>();
                break;
            case "Assassin":
                prefab.AddComponent<Assassin>();
                break;
            case "Tank":
                prefab.AddComponent<Tank>();
                break;
            case "Evader":
                prefab.AddComponent<Evader>();
                break;
            case "Specialist":
                prefab.AddComponent<Specialist>();
                break;
        }
        
        return prefab;
    }

    // Метод для получения команды игрока (нужен для других классов)
    List<GameObject> GetPlayerTeam()
    {
        List<GameObject> team = new List<GameObject>();
        foreach (BaseCharacter character in playerCharacters)
        {
            if (character != null && character.gameObject.activeInHierarchy)
            {
                team.Add(character.gameObject);
            }
        }
        return team;
    }

    // Метод для получения команды противников (нужен для других классов)
    List<GameObject> GetEnemyTeam()
    {
        List<GameObject> team = new List<GameObject>();
        foreach (BaseCharacter character in enemies)
        {
            if (character != null && character.gameObject.activeInHierarchy)
            {
                team.Add(character.gameObject);
            }
        }
        return team;
    }

    // Проверка активности боя
    public bool IsBattleActive
    {
        get { return currentState == BattleState.Battle; }
    }
    private float lastDebugTime = 0f;
    private int debugCounter = 0;
    private void DebugObjectsState()
    {
        if (Time.time - lastDebugTime < 5f) return;
        lastDebugTime = Time.time;
        
        LogDebug($"Время: {Time.time}, Состояние: {currentState}, Бой активен: {battleStarted}");
        
        if (startBattleButton != null)
        {
            LogDebug($"Кнопка старта боя: активна={startBattleButton.gameObject.activeInHierarchy}, интерактивна={startBattleButton.interactable}");
        }
        else
        {
            LogDebug("Кнопка старта боя не назначена");
        }
        
        LogDebug($"Персонажей размещено: {charactersPlaced}");
        LogDebug($"Врагов в списке: {enemies.Count}");
    }
    
    // Метод для отложенной активации вражеских контроллеров
private IEnumerator DelayedEnableController(EnemyController controller, string enemyName)
{
    // Ждем один кадр перед активацией
    yield return null;
    
    // Активируем контроллер
    controller.enabled = true;
    LogDebug($"Включен контроллер врага для {enemyName} с отложенной активацией");
}
    
    // Проверка состояния боя
    void CheckBattleState()
{
    // Добавим дополнительный дебаг-вывод для отслеживания
        if (debugCounter++ % 60 == 0) // Выводить примерно раз в секунду, если 60 fps
        {
            int aliveEnemies = 0;
            int alivePlayers = 0;
            
            foreach (BaseCharacter enemy in enemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.currentHealth > 0)
                    aliveEnemies++;
            }
            
            foreach (BaseCharacter character in playerCharacters)
            {
                if (character != null && character.gameObject.activeInHierarchy && character.currentHealth > 0)
                    alivePlayers++;
            }
            
            LogImportant($"Проверка состояния боя: Живы {alivePlayers} игроков и {aliveEnemies} врагов");
        }
    // Если бой не начался, даже не проверяем
    if (!battleStarted || !BattleManager.IsFightStarted)
    {
        return;
    }
    
    // Проверяем, есть ли живые враги
    bool allEnemiesDefeated = true;
    foreach (BaseCharacter enemy in enemies)
    {
        if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.currentHealth > 0)
        {
            allEnemiesDefeated = false;
            break;
        }
    }
    
    // Проверяем, есть ли живые персонажи игрока
    bool allPlayersDefeated = true;
    foreach (BaseCharacter character in playerCharacters)
    {
        if (character != null && character.gameObject.activeInHierarchy && character.currentHealth > 0)
        {
            allPlayersDefeated = false;
            break;
        }
    }
    
    // Определяем исход боя
    if (allEnemiesDefeated)
    {
        Victory();
    }
    else if (allPlayersDefeated)
    {
        Defeat();
    }
}
// Методы для логирования с фильтрацией
private void LogDebug(string message)
{
    if (enableDebugLogs)
        Debug.Log("[DEBUG] " + message);
}

private void LogWarning(string message)
{
    if (enableWarningLogs)
        Debug.LogWarning("[WARN] " + message);
}

private void LogError(string message)
{
    if (enableErrorLogs)
        Debug.LogError("[ERROR] " + message);
}

private void LogImportant(string message)
{
    if (enableImportantLogs)
        Debug.Log("[IMPORTANT] " + message);
}
private void FindAllTextComponents()
{
    // Ищем все Text компоненты в сцене
    UnityEngine.UI.Text[] allTexts = FindObjectsOfType<UnityEngine.UI.Text>();
    LogImportant("Найдено текстовых компонентов: " + allTexts.Length);
    
    foreach (var text in allTexts)
    {
        LogImportant($"Текст: '{text.text}' на объекте: {text.gameObject.name}");
    }
    
    // Ищем TextMeshPro компоненты
    TMPro.TextMeshProUGUI[] tmpTexts = FindObjectsOfType<TMPro.TextMeshProUGUI>();
    LogImportant("Найдено TMP текстов: " + tmpTexts.Length);
    
    foreach (var text in tmpTexts)
    {
        LogImportant($"TMP текст: '{text.text}' на объекте: {text.gameObject.name}");
    }
}

// Метод для настройки кнопок выбора сложности
private void SetupDifficultyButtons()
{
    // Ищем существующий Canvas или создаем новый
    Canvas canvas = FindObjectOfType<Canvas>();
    if (canvas == null)
    {
        GameObject canvasObj = new GameObject("DifficultyCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }
    
    // Если кнопка лёгкой сложности не назначена, создаём
    if (easyDifficultyButton == null)
    {
        easyDifficultyButton = CreateDifficultyButton(canvas, "Легкая сложность", new Vector2(0, 70), Color.green);
    }
    
    // Если кнопка средней сложности не назначена, создаём
    if (mediumDifficultyButton == null)
    {
        mediumDifficultyButton = CreateDifficultyButton(canvas, "Средняя сложность", new Vector2(0, 0), Color.yellow);
    }
    
    // Если кнопка сложной сложности не назначена, создаём
    if (hardDifficultyButton == null)
    {
        hardDifficultyButton = CreateDifficultyButton(canvas, "Кошмарная сложность", new Vector2(0, -70), Color.red);
    }
    
    // Добавляем обработчики событий для кнопок
    easyDifficultyButton.onClick.AddListener(() => SelectDifficulty(4)); // Легкая - 4 врага
    mediumDifficultyButton.onClick.AddListener(() => SelectDifficulty(5)); // Средняя - 5 врагов
    hardDifficultyButton.onClick.AddListener(() => SelectDifficulty(6)); // Кошмарная - 6 врагов
    
    // Отображаем сообщение с инструкцией
    if (tmpTextComponent != null)
    {
        tmpTextComponent.text = "Выберите сложность";
        tmpTextComponent.fontSize = 32;
        tmpTextComponent.color = Color.white;
        tmpTextComponent.gameObject.SetActive(true);
    }
    else if (placementInstructionText != null)
    {
        placementInstructionText.text = "Выберите сложность";
        placementInstructionText.fontSize = 32;
        placementInstructionText.color = Color.white;
        placementInstructionText.gameObject.SetActive(true);
    }
}

// Создание кнопки для выбора сложности
private UnityEngine.UI.Button CreateDifficultyButton(Canvas canvas, string text, Vector2 position, Color color)
{
    // Создаем игровой объект для кнопки
    GameObject buttonObj = new GameObject(text);
    buttonObj.transform.SetParent(canvas.transform, false);
    
    // Добавляем Image (фон кнопки)
    UnityEngine.UI.Image image = buttonObj.AddComponent<UnityEngine.UI.Image>();
    
    // Создаем цвет с небольшой прозрачностью
    color.a = 0.8f;
    image.color = color;
    
    // Добавляем компонент кнопки
    UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
    button.targetGraphic = image;
    
    // Настраиваем позицию и размер
    RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
    rectTransform.anchoredPosition = position;
    rectTransform.sizeDelta = new Vector2(300, 60);
    
    // Добавляем текст на кнопку
    GameObject textObj = new GameObject("Text");
    textObj.transform.SetParent(buttonObj.transform, false);
    
    // Пробуем найти TextMeshPro, если доступен
    TMPro.TextMeshProUGUI tmpText = null;
    try
    {
        tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 24;
        tmpText.color = Color.black;
        tmpText.alignment = TMPro.TextAlignmentOptions.Center;
    }
    catch (System.Exception)
    {
        // Если TextMeshPro недоступен, используем стандартный Text
        UnityEngine.Object.Destroy(tmpText);
        UnityEngine.UI.Text uiText = textObj.AddComponent<UnityEngine.UI.Text>();
        uiText.text = text;
        uiText.fontSize = 24;
        uiText.color = Color.black;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
    }
    
    // Настраиваем размер и позицию текста
    RectTransform textRectTransform = textObj.GetComponent<RectTransform>();
    textRectTransform.anchorMin = Vector2.zero;
    textRectTransform.anchorMax = Vector2.one;
    textRectTransform.sizeDelta = Vector2.zero;
    
    return button;
}

// Метод для выбора сложности
private void SelectDifficulty(int enemyCount)
{
    LogImportant("Выбрана сложность: " + enemyCount + " врагов");
    
    // Устанавливаем количество врагов
    numberOfEnemies = enemyCount;
    
    // Скрываем кнопки выбора сложности
    easyDifficultyButton.gameObject.SetActive(false);
    mediumDifficultyButton.gameObject.SetActive(false);
    hardDifficultyButton.gameObject.SetActive(false);
    
    // Отключаем фазу выбора сложности
    difficultySelectionPhase = false;
    
    // Спавним врагов с выбранной сложностью
    SpawnEnemies();
    
    // Начинаем размещение персонажей после выбора сложности
    StartCoroutine(PlacePlayerCharacters());
}

// Простая анимация масштабирования текста для победы
private IEnumerator VictoryTextAnimation(TMPro.TextMeshProUGUI text)
{
    float duration = 2.0f;
    float elapsed = 0f;
    float originalFontSize = text.fontSize;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float scale = 1f + 0.1f * Mathf.Sin(elapsed * 5f); // Колебание размера
        text.fontSize = originalFontSize * scale;
        yield return null;
    }
    
    // Возвращаем исходный размер
    text.fontSize = originalFontSize;
}

    // Метод для установки выбранных персонажей
    public void SetSelectedCharacters(List<string> characters)
    {
        selectedCharacters = characters;
        LogImportant($"Выбрано {selectedCharacters.Count} персонажей");
        
        foreach (string character in selectedCharacters)
        {
            LogDebug($"Выбран персонаж: {character}");
        }
    }

    // Метод для начала выбора сложности
    public void StartDifficultySelection()
    {
        LogImportant("Переход к выбору сложности");
        
        // Скрываем панель выбора персонажей
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(false);
        }
        
        // Показываем панель выбора сложности
        difficultySelectionPhase = true;
        SetupDifficultyButtons();
    }

    // Создание персонажей игрока
    void CreatePlayerCharacters()
    {
        // Очищаем существующий список персонажей
        playerCharacters.Clear();
        
        // Если нет выбранных персонажей, используем набор по умолчанию
        if (selectedCharacters.Count == 0)
        {
            LogWarning("Нет выбранных персонажей, используем стандартный набор");
            
            // Создаем воина
            CreateCharacterByType("Воин");
            
            // Создаем стрелка
            CreateCharacterByType("Лучник");
            
            // Создаем персонажа поддержки
            CreateCharacterByType("Поддержка");
            
            // Создаем убийцу
            CreateCharacterByType("Убийца");
        }
        else
        {
            // Создаем выбранных персонажей
            foreach (string characterType in selectedCharacters)
            {
                CreateCharacterByType(characterType);
            }
        }
        
        LogDebug($"Создано {playerCharacters.Count} персонажей для игрока");
    }

    // Вспомогательный метод для создания персонажей по типу
    private void CreateCharacterByType(string characterType)
    {
        GameObject characterPrefab = null;
        
        // Определяем префаб в зависимости от типа персонажа
        switch (characterType)
        {
            case "Воин":
                characterPrefab = warriorPrefab;
                break;
            case "Лучник":
                characterPrefab = archerPrefab;
                break;
            case "Поддержка":
                characterPrefab = supportPrefab;
                break;
            case "Убийца":
                characterPrefab = assassinPrefab;
                break;
            case "Танк":
                characterPrefab = tankPrefab;
                break;
            case "Уклонист":
                characterPrefab = evaderPrefab;
                break;
            case "Специалист":
                characterPrefab = specialistPrefab;
                break;
        }
        
        if (characterPrefab != null)
        {
            // Создаем персонажа
            GameObject character = Instantiate(characterPrefab);
            character.name = characterType;
            character.tag = "Player";
            
            // Получаем и настраиваем BaseCharacter компонент
            BaseCharacter characterComponent = character.GetComponent<BaseCharacter>();
            if (characterComponent != null)
            {
                characterComponent.isEnemy = false;
                playerCharacters.Add(characterComponent);
                AddHealthBar(character);
            }
            
            // Деактивируем персонажа до фазы размещения
            character.SetActive(false);
            
            LogDebug($"Создан персонаж {characterType}");
        }
        else
        {
            LogError($"Не найден префаб для типа персонажа: {characterType}");
        }
    }
}