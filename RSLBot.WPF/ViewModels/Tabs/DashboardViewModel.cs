using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using RSLBot.Core.CoreHelpers;
using RSLBot.Core.Interfaces;
using RSLBot.Core.Services;
using RSLBot.Shared.Settings;
using AppContext = RSLBot.Core.Services.AppContext;

namespace RSLBot.WPF.ViewModels.Tabs
{
    /// <summary>
    /// ViewModel для головної вкладки "Послідовність сценаріїв", що керує чергою виконання сценаріїв.
    /// </summary>
    public class DashboardViewModel : ReactiveViewModelBase
    {
        private readonly Dictionary<IScenario.ScenarioId, IScenario> _scenarios;
        private readonly SettingsService _settingsService;
        private readonly ScreenCaptureManager _screenCaptureManager;
        private readonly Tools _tools;
        private readonly SharedSettings _sharedSettings;
        private readonly ILoggingService _loggingService;
        
        private ScenarioQueueItem? _selectedQueueItem;
        private bool _isRunning;
        private const string SettingsFileName = "dashboard_settings.json";

        private CancellationTokenSource cancellationTokenSource;
        
        public ObservableCollection<ScenarioQueueItem> ScenarioQueue { get; } = new();
        
        public List<AvailableScenario> AvailableScenarios { get; }

        public ScenarioQueueItem? SelectedQueueItem
        {
            get => _selectedQueueItem;
            set => this.RaiseAndSetIfChanged(ref _selectedQueueItem, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }

        public ReactiveCommand<IScenario.ScenarioId, Unit> AddScenarioCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveSelectedCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveUpCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveDownCommand { get; }
        public ReactiveCommand<Unit, Unit> RunSequenceCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelSequenceCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearQueueCommand { get; }

        public DashboardViewModel(
            IEnumerable<IScenario> scenarios,
            SettingsService settingsService,
            ScreenCaptureManager screenCaptureManager,
            Tools tools,
            SharedSettings sharedSettings,
            ILoggingService loggingService)
        {
            _scenarios = scenarios.ToDictionary(s => s.Id, s => s);
            _settingsService = settingsService;
            _screenCaptureManager = screenCaptureManager;
            _tools = tools;
            _sharedSettings = sharedSettings;
            _loggingService = loggingService;

            // Список доступних сценаріїв
            AvailableScenarios =
            [
                new() { Id = IScenario.ScenarioId.ClassicArena, DisplayName = "Класична Арена" },
                new() { Id = IScenario.ScenarioId.TagArena, DisplayName = "Тег Арена" },
                new() { Id = IScenario.ScenarioId.Twins, DisplayName = "Залізні Близнюки" },
                new() { Id = IScenario.ScenarioId.Minotaur, DisplayName = "Лабіринт Мінотавра" }
            ];

            var queueChanged = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => ScenarioQueue.CollectionChanged += handler,
                handler => ScenarioQueue.CollectionChanged -= handler)
                .Select(_ => Unit.Default)
                .StartWith(Unit.Default);

            // Команди
            AddScenarioCommand = ReactiveCommand.Create<IScenario.ScenarioId>(AddScenario);
            
            var canRemove = this.WhenAnyValue(x => x.SelectedQueueItem).Select(item => item != null);
            RemoveSelectedCommand = ReactiveCommand.Create(RemoveSelected, canRemove);
            
            var canMoveUp = this.WhenAnyValue(x => x.SelectedQueueItem)
                .CombineLatest(queueChanged, (item, _) => item != null && ScenarioQueue.IndexOf(item) > 0);
            MoveUpCommand = ReactiveCommand.Create(MoveUp, canMoveUp);
            
            var canMoveDown = this.WhenAnyValue(x => x.SelectedQueueItem)
                .CombineLatest(queueChanged, (item, _) => item != null && ScenarioQueue.IndexOf(item) < ScenarioQueue.Count - 1);
            MoveDownCommand = ReactiveCommand.Create(MoveDown, canMoveDown);
            
            var canRunOrClear = this.WhenAnyValue(x => x.IsRunning)
                .CombineLatest(queueChanged, (running, _) => !running && ScenarioQueue.Any());
            RunSequenceCommand = ReactiveCommand.CreateFromTask(RunSequenceAsync, canRunOrClear, outputScheduler: RxApp.MainThreadScheduler);
            
            var canCancel = this.WhenAnyValue(x => x.IsRunning);
            CancelSequenceCommand = ReactiveCommand.Create(CancelSequence);
            
            ClearQueueCommand = ReactiveCommand.Create(ClearQueue, canRunOrClear);

            // Обробка помилок
            RunSequenceCommand.ThrownExceptions.Subscribe(HandleException);

            // Завантажити налаштування
            LoadSettings();
        }

        private void AddScenario(IScenario.ScenarioId scenarioId)
        {
            var scenario = AvailableScenarios.FirstOrDefault(s => s.Id == scenarioId);
            if (scenario != null)
            {
                ScenarioQueue.Add(new ScenarioQueueItem
                {
                    ScenarioId = scenarioId,
                    DisplayName = scenario.DisplayName,
                    IsEnabled = true
                });
                SaveSettings();
            }
        }

        private void RemoveSelected()
        {
            if (SelectedQueueItem != null)
            {
                ScenarioQueue.Remove(SelectedQueueItem);
                SaveSettings();
            }
        }

        private void MoveUp()
        {
            if (SelectedQueueItem != null)
            {
                var index = ScenarioQueue.IndexOf(SelectedQueueItem);
                if (index > 0)
                {
                    ScenarioQueue.Move(index, index - 1);
                    SaveSettings();
                }
            }
        }

        private void MoveDown()
        {
            if (SelectedQueueItem != null)
            {
                var index = ScenarioQueue.IndexOf(SelectedQueueItem);
                if (index < ScenarioQueue.Count - 1)
                {
                    ScenarioQueue.Move(index, index + 1);
                    SaveSettings();
                }
            }
        }

        private void ClearQueue()
        {
            ScenarioQueue.Clear();
            SaveSettings();
        }

        private async Task RunSequenceAsync()
        {
            IsRunning = true;
            cancellationTokenSource = new CancellationTokenSource();
            _loggingService.InfoUi("Початок виконання послідовності сценаріїв...");

            try
            {
                // Перевірка захоплення екрана
                bool selected = await _screenCaptureManager.EnsureCaptureIsActiveAsync(AppContext.MainWindowHandle);
                if (!selected)
                {
                    System.Windows.MessageBox.Show("Вибір вікна було скасовано.");
                    return;
                }

                if (_tools.raidProcess.MainWindowHandle != _screenCaptureManager.CapturedWindowHandle)
                {
                    System.Windows.MessageBox.Show("Процес гри не знайдено або його вікно недоступне, або ви обрали не вікно з грою.");
                    return;
                }

                var bitmap = await _screenCaptureManager.CaptureFrameAsync();
                bitmap.Save("test.png", ImageFormat.Png);

                // Виконання черги
                foreach (var item in ScenarioQueue.Where(i => i.IsEnabled))
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _loggingService.InfoUi("Послідовність скасовано користувачем.");
                        break;
                    }

                    _loggingService.InfoUi($"Виконання сценарію: {item.DisplayName}");

                    if (_scenarios.TryGetValue(item.ScenarioId, out var scenario))
                    {
                        await scenario.ExecuteAsync();
                    }

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }
                }

                if (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _loggingService.InfoUi("Послідовність сценаріїв завершена успішно!");
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Помилка при виконанні послідовності: {ex.Message}");
                throw;
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void CancelSequence()
        {
            _loggingService.InfoUi("Скасування послідовності сценаріїв...");
        }

        private void SaveSettings()
        {
            var settings = new DashboardSettings
            {
                ScenarioQueue = ScenarioQueue.ToList(),
            };
            _settingsService.SaveSettings(SettingsFileName, settings);
        }

        private void LoadSettings()
        {
            var settings = _settingsService.LoadSettings<DashboardSettings>(SettingsFileName);
            if (settings != null && settings.ScenarioQueue.Any())
            {
                ScenarioQueue.Clear();
                foreach (var item in settings.ScenarioQueue)
                {
                    ScenarioQueue.Add(item);
                }
            }
        }

        private void HandleException(Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Виникла помилка під час виконання послідовності:\n\n{ex.Message}",
                "Помилка",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    /// <summary>
    /// Доступний сценарій для вибору.
    /// </summary>
    public class AvailableScenario
    {
        public IScenario.ScenarioId Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
