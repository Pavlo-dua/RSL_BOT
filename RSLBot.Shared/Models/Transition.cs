namespace RSLBot.Shared.Models;

/// <summary>
/// Описує перехід на інший екран. Містить пряме посилання на цільовий вузол графа.
/// </summary>
public class Transition
{
    /// <summary>
    /// Чи потрібно шукати елемент по горизонталі.
    /// </summary>
    public bool HorizontalSearch { get; set; }

    /// <summary>
    /// Пряме посилання на об'єкт цільового екрана.
    /// </summary>
    public ScreenDefinition TargetScreen { get; set; }

    /// <summary>
    /// Назва елемента, що ініціює перехід.
    /// </summary>
    public UIElement TriggerElement { get; set; }
}