using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaster.App.ViewModels;

public partial class ReplViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = string.Empty;
    
    public ObservableCollection<ReplMessage> Messages { get; } = new();

    private readonly Dictionary<string, (string Description, string Usage)> _commands = new()
    {
        { "help", ("Show this help or detailed help for a specific command", "help [command]") },
        { "clear", ("Clear terminal output", "clear") },
        { "player", ("Show player profile", "player") },
        { "points", ("Manage or view points", "points [award|spend] <exp|coins> <amount>") },
        { "inventory", ("Manage inventory items", "inventory [list|add|remove] [name] [qty]") },
        { "apps", ("Manage applications", "apps [list|revoke] [app_id]") },
        { "perms", ("Manage application permissions", "perms <app_id> [grant|revoke] [resource] [action]") },
        { "sync", ("Device synchronization", "sync [status|pair|unpair] [device_id]") }
    };

    [RelayCommand]
    private void Execute(string? inputParam)
    {
        var textToUse = !string.IsNullOrWhiteSpace(inputParam) ? inputParam : InputText;
        if (string.IsNullOrWhiteSpace(textToUse)) return;
        
        var input = textToUse.Trim();
        Messages.Add(new ReplMessage { Text = input, IsCommandEcho = true }); // bash-like prompt
        InputText = string.Empty;
        
        ProcessCommand(input);
    }
    
    public event EventHandler? CommandExecuted;
    
    private async void ProcessCommand(string input)
    {
        try
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var cmd = parts[0].ToLowerInvariant();
            
            if (cmd == "help")
            {
                if (parts.Length > 1)
                {
                    cmd = parts[1].ToLowerInvariant();
                    parts = new[] { cmd, "-h" };
                }
                else
                {
                    HandleHelp(parts);
                    return;
                }
            }

            if (parts.Any(p => p == "-h" || p == "--help"))
            {
                HandleHelp(new[] { "help", cmd });
                return;
            }
            
            var playerService = AppHost.Services.GetService<GameMaster.Core.Services.IPlayerService>();
            var pointsService = AppHost.Services.GetService<GameMaster.Core.Services.IPointsService>();
            var inventoryService = AppHost.Services.GetService<GameMaster.Core.Services.IInventoryService>();
            var appRegistryService = AppHost.Services.GetService<GameMaster.Core.Services.IAppRegistryService>();

            switch (cmd)
            {
                case "clear":
                    Messages.Clear();
                    break;
                case "player":
                    if (playerService != null)
                    {
                        var player = await playerService.GetPlayerAsync();
                        if (player != null)
                            Messages.Add(new ReplMessage { Text = $"Player: {player.DisplayName}", Color = "#D4D4D4" });
                    }
                    break;
                case "points":
                    if (pointsService != null)
                    {
                        if (parts.Length == 1)
                        {
                            var points = await pointsService.GetBalancesAsync();
                            foreach(var p in points)
                                Messages.Add(new ReplMessage { Text = $"{p.CurrencyId}: {p.Balance}", Color = "#FCE94F" });
                        }
                        else if (parts.Length >= 4)
                        {
                            var action = parts[1].ToLowerInvariant();
                            var resource = parts[2].ToLowerInvariant(); // e.g. "exp_points" or "coins"
                            var amount = int.Parse(parts[3]);
                            if (action == "award") await pointsService.AwardPointsAsync(resource, amount);
                            else if (action == "spend") await pointsService.SpendPointsAsync(resource, amount);
                            Messages.Add(new ReplMessage { Text = "Points updated.", Color = "#8AE234" });
                        }
                    }
                    break;
                case "inventory":
                    if (inventoryService != null)
                    {
                        if (parts.Length == 1 || parts[1].ToLowerInvariant() == "list")
                        {
                            var inv = await inventoryService.GetItemsAsync();
                            foreach(var i in inv)
                                Messages.Add(new ReplMessage { Text = $"{i.Name}: {i.Quantity}", Color = "#D4D4D4" });
                        }
                        else if (parts.Length >= 4)
                        {
                            var action = parts[1].ToLowerInvariant();
                            if (action == "add") await inventoryService.AddItemAsync(parts[2], int.Parse(parts[3]), "", "system");
                            else if (action == "remove") await inventoryService.RemoveItemAsync(Guid.Parse(parts[2]), int.Parse(parts[3]));
                            Messages.Add(new ReplMessage { Text = "Inventory updated.", Color = "#8AE234" });
                        }
                    }
                    break;
                case "apps":
                    if (appRegistryService != null)
                    {
                        if (parts.Length == 1 || parts[1].ToLowerInvariant() == "list")
                        {
                            var apps = await appRegistryService.ListAppsAsync();
                            foreach(var app in apps)
                                Messages.Add(new ReplMessage { Text = $"{app.AppId}: {app.AppName} ({app.Platform})", Color = "#D4D4D4" });
                        }
                        else if (parts.Length >= 3 && parts[1].ToLowerInvariant() == "revoke")
                        {
                            await appRegistryService.DeregisterAppAsync(Guid.Parse(parts[2]));
                            Messages.Add(new ReplMessage { Text = "App revoked.", Color = "#8AE234" });
                        }
                    }
                    break;

                case "sync":
                    Messages.Add(new ReplMessage { Text = "Command execution pending.", Color = "#FCE94F" });
                    break;
                default:
                    Messages.Add(new ReplMessage { Text = $"GameMaster: {cmd}: command not found", Color = "#EF2929" });
                    break;
            }
            
            CommandExecuted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Messages.Add(new ReplMessage { Text = $"Error: {ex.Message}", Color = "#EF2929" });
        }
    }

    private void HandleHelp(string[] parts)
    {
        if (parts.Length > 1)
        {
            var topic = parts[1].ToLowerInvariant();
            if (_commands.TryGetValue(topic, out var info))
            {
                Messages.Add(new ReplMessage { Text = $"{topic} - {info.Description}\nUsage: {info.Usage}", Color = "#D4D4D4" });
            }
            else
            {
                Messages.Add(new ReplMessage { Text = $"help: no help topics match '{topic}'.", Color = "#EF2929" });
            }
        }
        else
        {
            var helpText = "GameMaster shell, version 1.0.0-release\nAvailable commands:\n";
            foreach (var kvp in _commands)
            {
                helpText += $"  {kvp.Key,-10} {kvp.Value.Description}\n";
            }
            helpText += "Type 'help name' to find out more about the function 'name'.";
            Messages.Add(new ReplMessage { Text = helpText, Color = "#D4D4D4" });
        }
    }
}

public class ReplMessage
{
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = "#D4D4D4";
    public bool IsCommandEcho { get; set; }
}
