using System.Windows.Automation;

namespace ParadowSync.Automation.Services;

public sealed class UiAutomationCharacterSelector : ICharacterSelector
{
    private static readonly PropertyCondition IsListItemCondition =
        new(AutomationElement.ControlTypeProperty, ControlType.ListItem);

    private static readonly PropertyCondition IsListCondition =
        new(AutomationElement.ControlTypeProperty, ControlType.List);

    public Task<bool> SelectCharacterAsync(
        nint gameHwnd,
        string characterName,
        TimeSpan timeout,
        CancellationToken ct)
    {
        return Task.Run(
            () => SelectCharacterCore(gameHwnd, characterName, timeout, ct),
            ct);
    }

    private static bool SelectCharacterCore(
        nint gameHwnd,
        string characterName,
        TimeSpan timeout,
        CancellationToken ct)
    {
        try
        {
            var root = AutomationElement.FromHandle(gameHwnd);
            if (root is null)
                return false;

            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();

                var characterElement = FindCharacterElement(root, characterName);
                if (characterElement is not null && TryActivateElement(characterElement))
                    return true;

                Thread.Sleep(200);
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static AutomationElement? FindCharacterElement(AutomationElement root, string characterName)
    {
        var nameCondition = new PropertyCondition(AutomationElement.NameProperty, characterName);
        var element = root.FindFirst(TreeScope.Descendants, nameCondition);
        if (element is not null)
            return element;

        foreach (var list in root.FindAll(TreeScope.Descendants, IsListCondition).Cast<AutomationElement>())
        {
            foreach (var item in list.FindAll(TreeScope.Children, IsListItemCondition).Cast<AutomationElement>())
            {
                if (string.Equals(item.Current.Name, characterName, StringComparison.Ordinal))
                    return item;
            }
        }

        foreach (var item in root.FindAll(TreeScope.Descendants, IsListItemCondition).Cast<AutomationElement>())
        {
            if (string.Equals(item.Current.Name, characterName, StringComparison.Ordinal))
                return item;
        }

        return null;
    }

    private static bool TryActivateElement(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
        {
            ((InvokePattern)invokePattern).Invoke();
            return true;
        }

        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPattern))
        {
            ((SelectionItemPattern)selectionPattern).Select();
            return true;
        }

        return false;
    }
}
