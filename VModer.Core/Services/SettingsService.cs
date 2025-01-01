using VModer.Core.Models;

namespace VModer.Core.Services;

public sealed class SettingsService
{
    public string GameRootFolderPath { get; set; } = string.Empty;
    public string ModRootFolderPath { get; set; } = string.Empty;
    // TODO: 在VS Code 中添加配置项
    public GameLanguage GameLanguage { get; set; } = GameLanguage.Default;
}