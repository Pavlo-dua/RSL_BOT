# RSLBot Architecture

This project is an automation utility for "Raid: Shadow Legends" built with .NET and WPF.

## Project Structure

The solution is divided into several projects to ensure separation of concerns, maintainability, and extensibility.

-   `RSLBot.sln`: The main solution file.

### /RSLBot.Shared
Contains shared models and data structures used across all projects.
-   `Models/ScreenDefinition.cs`: Defines the structure for describing a game screen and its UI elements.
-   `Models/ScenarioSettings.cs`: Defines the structure for configuring a farming scenario.

### /RSLBot.Screenshot
Responsible for capturing the game's screen.
-   `IScreenCaptureService.cs`: An interface for the screen capture functionality.
-   `ScreenCaptureService.cs`: A **placeholder implementation**. You need to replace the logic in this class with your own DirectX-compatible screen capture code.

### /RSLBot.Core
The heart of the application, containing all the core logic.
-   `Interfaces/IScenario.cs`: A common interface for all farming scenarios.
-   `Services/ScenarioExecutor.cs`: The main class that manages and executes scenarios.
-   `Services/ScreenIdentifier.cs`: A placeholder class for identifying the current game screen.
-   `Services/Navigator.cs`: A placeholder class for navigating between game screens.

### /RSLBot.WPF
The desktop user interface.
-   `MainWindow.xaml`: The main application window.
-   `ViewModels/MainViewModel.cs`: The view model for the main window, connecting the UI to the core logic.

### /RSLBot.Telegram
Handles integration with a Telegram bot for remote commands.
-   `TelegramBotManager.cs`: A class to manage the bot's lifecycle and command handling.

### /Configuration
Contains external JSON configuration files.
-   `screens.json`: A file to define game screens and their UI elements.
-   `scenarios.json`: A file to configure the available farming scenarios.

## Getting Started

1.  Open `RSLBot.sln` in Visual Studio 2022 or later.
2.  Restore the NuGet packages if they are not restored automatically.
3.  **Crucially**: Navigate to `RSLBot.Screenshot/ScreenCaptureService.cs` and replace the placeholder logic with your actual screen capture implementation.
4.  Build and run the `RSLBot.WPF` project.
