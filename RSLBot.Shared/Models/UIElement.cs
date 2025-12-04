namespace RSLBot.Shared.Models;

using System.Drawing;

/// <summary>
/// Описує елемент інтерфейсу на екрані.
/// </summary>
public class UIElement
{
    public string Name { get; set; }
    public Rectangle Area { get; set; }
    public string ImageTemplatePath { get; set; }
    public string? Key { get; set; }

}