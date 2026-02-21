using System.Windows.Media;

namespace Arcmark.Services;

/// <summary>
/// Represents a detected browser on the current machine.
/// </summary>
public record BrowserInfo(string Name, string ExecutablePath, ImageSource? Icon);
