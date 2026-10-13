public interface IInteractable
{
    /// <summary>
    /// Текст подсказки на экране, например: "Открыть дверь (E)", "Подобрать (E)"
    /// </summary>
    string PromptMessage { get; }

    /// <summary>
    /// Действие, которое происходит при нажатии E
    /// </summary>
    void Interact(PlayerInteraction player);
}
