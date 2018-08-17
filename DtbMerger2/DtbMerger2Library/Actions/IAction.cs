namespace DtbMerger2Library.Actions
{
    /// <summary>
    /// Generic interface for an action, that support undo/redo
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Executes the action
        /// </summary>
        void Execute();

        /// <summary>
        /// Un-executes the action (aka un-does)
        /// </summary>
        void UnExecute();

        /// <summary>
        /// A <see cref="bool"/> indicating if the action can be executed
        /// </summary>
        bool CanExecute { get; }

        /// <summary>
        /// A <see cref="bool"/> indicating if the action can be un-executed
        /// </summary>
        bool CanUnExecute { get; }

        /// <summary>
        /// A description of the action, suitable for user display
        /// </summary>
        string Description { get; }
    }
}
