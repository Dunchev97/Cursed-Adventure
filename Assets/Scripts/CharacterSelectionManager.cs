using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterSlot
    {
        public TMP_Dropdown dropdown;
        public Image characterImage;
        public GameObject selectedCharacterIndicator;
    }

    // Ссылка на BattleManager
    public BattleManager battleManager;
    
    // Слоты для выбора персонажей
    public CharacterSlot[] characterSlots = new CharacterSlot[6];
    
    // Панель выбора персонажей
    public GameObject characterSelectionPanel;
    
    // Кнопка готовности
    public Button readyButton;
    
    // Доступные классы персонажей
    private List<string> availableCharacters = new List<string>
    {
        "Воин",
        "Лучник",
        "Поддержка",
        "Убийца",
        "Танк",
        "Прыгун",
        "Специалист"
    };
    
    // Список выбранных персонажей
    private List<string> selectedCharacters = new List<string>();
    
    // Start вызывается перед первым кадром
    void Start()
    {
        // Инициализируем выпадающие списки
        InitializeDropdowns();
        
        // Добавляем обработчик события для кнопки "Готов"
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClick);
        }
        else
        {
            Debug.LogError("Кнопка Готов не назначена!");
        }
        
        // Если не назначен BattleManager, найдем его
        if (battleManager == null)
        {
            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                Debug.LogError("BattleManager не найден в сцене!");
            }
        }
    }
    
    // Инициализация выпадающих списков
    void InitializeDropdowns()
    {
        // Создаем опции для выпадающих списков
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        
        // Добавляем опцию "Не выбрано"
        options.Add(new TMP_Dropdown.OptionData("Не выбрано"));
        
        // Добавляем доступные классы персонажей
        foreach (string character in availableCharacters)
        {
            options.Add(new TMP_Dropdown.OptionData(character));
        }
        
        // Инициализируем каждый выпадающий список
        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (characterSlots[i].dropdown != null)
            {
                // Очищаем опции
                characterSlots[i].dropdown.ClearOptions();
                
                // Добавляем новые опции
                characterSlots[i].dropdown.AddOptions(options);
                
                // Устанавливаем начальное значение "Не выбрано"
                characterSlots[i].dropdown.value = 0;
                
                // Индекс слота для использования в лямбда-выражении
                int slotIndex = i;
                
                // Добавляем обработчик события изменения значения
                characterSlots[i].dropdown.onValueChanged.AddListener((int value) => OnDropdownValueChanged(slotIndex, value));
                
                // Скрываем индикатор выбранного персонажа
                if (characterSlots[i].selectedCharacterIndicator != null)
                {
                    characterSlots[i].selectedCharacterIndicator.SetActive(false);
                }
            }
        }
    }
    
    // Обработчик изменения значения в выпадающем списке
    void OnDropdownValueChanged(int slotIndex, int value)
    {
        // Если выбрано "Не выбрано"
        if (value == 0)
        {
            // Скрываем индикатор выбранного персонажа
            if (characterSlots[slotIndex].selectedCharacterIndicator != null)
            {
                characterSlots[slotIndex].selectedCharacterIndicator.SetActive(false);
            }
            
            // Убираем персонажа из списка выбранных, если он был добавлен ранее
            if (slotIndex < selectedCharacters.Count && selectedCharacters[slotIndex] != null)
            {
                selectedCharacters[slotIndex] = null;
            }
        }
        else
        {
            // Получаем выбранного персонажа
            string selectedCharacter = availableCharacters[value - 1]; // -1 потому что первая опция "Не выбрано"
            
            // Если список выбранных персонажей еще не достаточно большой
            while (selectedCharacters.Count <= slotIndex)
            {
                selectedCharacters.Add(null);
            }
            
            // Устанавливаем выбранного персонажа
            selectedCharacters[slotIndex] = selectedCharacter;
            
            // Показываем индикатор выбранного персонажа
            if (characterSlots[slotIndex].selectedCharacterIndicator != null)
            {
                characterSlots[slotIndex].selectedCharacterIndicator.SetActive(true);
            }
            
            // Устанавливаем изображение персонажа
            UpdateCharacterImage(slotIndex, selectedCharacter);
        }
        
        // Обновляем статус кнопки "Готов"
        UpdateReadyButtonStatus();
    }
    
    // Обновление изображения персонажа
    void UpdateCharacterImage(int slotIndex, string characterName)
    {
        if (characterSlots[slotIndex].characterImage != null)
        {
            // В реальном проекте здесь будет логика загрузки изображения персонажа
            // Для прототипа меняем цвет в зависимости от типа персонажа
            Color characterColor = Color.white;
            
            switch (characterName)
            {
                case "Воин":
                    characterColor = Color.blue;
                    break;
                case "Лучник":
                    characterColor = Color.green;
                    break;
                case "Поддержка":
                    characterColor = Color.yellow;
                    break;
                case "Убийца":
                    characterColor = Color.magenta;
                    break;
                case "Танк":
                    characterColor = new Color(0.7f, 0.4f, 0.1f); // Коричневый
                    break;
                case "Прыгун":
                    characterColor = new Color(0.1f, 0.8f, 0.3f); // Светло-зеленый
                    break;
                case "Специалист":
                    characterColor = new Color(0.2f, 0.5f, 1f); // Голубой
                    break;
            }
            
            characterSlots[slotIndex].characterImage.color = characterColor;
        }
    }
    
    // Обновление статуса кнопки "Готов"
    void UpdateReadyButtonStatus()
    {
        // Проверяем, есть ли хотя бы один выбранный персонаж
        bool hasSelectedCharacter = false;
        
        foreach (string character in selectedCharacters)
        {
            if (!string.IsNullOrEmpty(character))
            {
                hasSelectedCharacter = true;
                break;
            }
        }
        
        // Активируем кнопку "Готов", если есть хотя бы один выбранный персонаж
        if (readyButton != null)
        {
            readyButton.interactable = hasSelectedCharacter;
        }
    }
    
    // Обработчик нажатия на кнопку "Готов"
    void OnReadyButtonClick()
    {
        // Скрываем панель выбора персонажей
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(false);
        }
        
        // Передаем список выбранных персонажей в BattleManager
        if (battleManager != null)
        {
            // Создаем список только с выбранными персонажами (без null)
            List<string> finalSelectedCharacters = new List<string>();
            
            foreach (string character in selectedCharacters)
            {
                if (!string.IsNullOrEmpty(character))
                {
                    finalSelectedCharacters.Add(character);
                }
            }
            
            // Вызываем метод в BattleManager для установки выбранных персонажей
            battleManager.SetSelectedCharacters(finalSelectedCharacters);
            
            // Переходим к выбору сложности
            battleManager.StartDifficultySelection();
        }
        else
        {
            Debug.LogError("BattleManager не найден при нажатии кнопки Готов!");
        }
    }
}