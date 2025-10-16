# Рефакторинг ArenaFarmingScenario

## Огляд

Було проведено рефакторинг коду для винесення загальної логіки з `ArenaFarmingScenario` та `ArenaTagFarmingScenario` в базовий клас `ArenaFarmingScenarioBase`. Це дозволяє уникнути дублювання коду та спрощує підтримку.

## Структура

### Базовий клас
- `ArenaFarmingScenarioBase<T>` - абстрактний базовий клас з усією загальною логікою
- `ArenaConfiguration` - клас конфігурації для налаштування координат та ресурсів

### Конкретні реалізації
- `ArenaFarmingScenario` - для Classic Arena
- `ArenaTagFarmingScenario` - для Arena Tag (з прикладом різних координат)

## ArenaConfiguration

Клас `ArenaConfiguration` містить всі налаштування, які можуть відрізнятися між різними типами арени:

### Координати прокрутки
```csharp
public int ScrollDragX { get; set; } = 542;
public int ScrollDragStartYBottom { get; set; } = 613;
public int ScrollDragStartYBottomUp { get; set; } = 208;
public int ScrollDragEndYBottomUp { get; set; } = 534;
public int ScrollDragEndYTop { get; set; } = 155;
```

### Координати кнопки старту
```csharp
public int StartButtonX { get; set; } = 940;
public int StartButtonWidth { get; set; } = 200;
```

### Області для перевірки
```csharp
public Rectangle ScrollCheckRect { get; set; } = new Rectangle(228, 540, 207, 88);
public Point ControlSnapshotPoint { get; set; } = new Point(228, 540);
```

### Область опонента
```csharp
public int OpponentAreaX { get; set; } = 228;
public int OpponentAreaWidth { get; set; } = 207;
public int OpponentAreaHeight { get; set; } = 85;
public int OpponentAreaOffsetY { get; set; } = 21;
```

### Шляхи до ресурсів
```csharp
public string StartButtonImagePath { get; set; } = @"Configuration\Ukr\Templates\ArenaClassic\arena_classic_start.png";
public string AddResourcesImagePath { get; set; } = @"Configuration\ScreenDefinition\Templates\add_resources.png";
```

### Координати для читання токенів
```csharp
public Rectangle TokenFullArea { get; set; } = new Rectangle(818, 13, 16, 20);
public Rectangle TokenTextAreaFull { get; set; } = new Rectangle(841, 13, 52, 22);
public Rectangle TokenTextAreaNormal { get; set; } = new Rectangle(852, 13, 41, 22);
```

### ScreenDefinitionId для різних екранів
```csharp
public ScreenDefinitionId MainScreenId { get; set; } = ScreenDefinitionId.ClassicArena;
public ScreenDefinitionId FreeTokensScreenId { get; set; } = ScreenDefinitionId.ClassicArenaFreeTokens;
public ScreenDefinitionId BuyTokensScreenId { get; set; } = ScreenDefinitionId.ClassicArenaBuyTokens;
public ScreenDefinitionId PreparingScreenId { get; set; } = ScreenDefinitionId.ClassicArenaPreparing;
public ScreenDefinitionId DefeatScreenId { get; set; } = ScreenDefinitionId.ClassicArenaDefeat;
public ScreenDefinitionId VictoryScreenId { get; set; } = ScreenDefinitionId.ClassicArenaVin;
public ScreenDefinitionId FightScreenId { get; set; } = ScreenDefinitionId.ClassicArenaFight;
```

## Як створити новий сценарій арени

1. Створіть новий клас, що наслідується від `ArenaFarmingScenarioBase<ArenaFarmingSettings>`
2. Перевизначте властивість `Configuration` з потрібними налаштуваннями
3. Перевизначте `MainFarmingScreenId` та `Id` якщо потрібно

### Приклад:
```csharp
public class NewArenaFarmingScenario : ArenaFarmingScenarioBase<ArenaFarmingSettings>
{
    protected override ScreenDefinitionId MainFarmingScreenId => ScreenDefinitionId.NewArena;
    public override IScenario.ScenarioId Id => IScenario.ScenarioId.NewArena;

    protected override ArenaConfiguration Configuration => new ArenaConfiguration
    {
        // Налаштуйте координати та ресурси для нового типу арени
        ScrollDragStartYBottom = 650, // Інші координати
        StartButtonImagePath = @"Configuration\Ukr\Templates\NewArena\new_arena_start.png",
        MainScreenId = ScreenDefinitionId.NewArena,
        // ... інші налаштування
    };
    
    public NewArenaFarmingScenario(INavigator navigator, ArenaFarmingSettings settings, ILoggingService logger, Tools tools, ImageAnalyzer imageAnalyzer, SharedSettings sharedSettings, ImageResourceManager imageResourceManager)
        : base(navigator, settings, logger, tools, imageAnalyzer, sharedSettings, imageResourceManager)
    {
    }
}
```

## Переваги рефакторингу

1. **Усунення дублювання коду** - вся логіка тепер в одному місці
2. **Легкість підтримки** - зміни в логіці потрібно робити тільки в базовому класі
3. **Гнучкість налаштування** - легко створювати нові типи арени з різними координатами
4. **Типобезпека** - конфігурація типізована та перевіряється на етапі компіляції
5. **Читабельність** - код стає більш зрозумілим та структурованим

## Групування координат

Координати можна групувати за призначенням:
- **Координати прокрутки** - для управління прокруткою списку опонентів
- **Координати кнопок** - для кліків по кнопках
- **Області перевірки** - для визначення стану екрану
- **Області опонентів** - для сканування та ідентифікації опонентів
- **Координати токенів** - для читання кількості токенів

Це робить конфігурацію більш зрозумілою та легшою для налаштування.
