namespace BlazorApp.Services
{
    /// <summary>
    /// Simple interface to show dialogs.
    /// </summary>
    public interface IWindowService
    {
        /// <summary>
        /// Shows a simple message dialog.
        /// </summary>
        /// <param name="message">message to show ind dialog.</param>
        void ShowMessage(string message);
    }
}
