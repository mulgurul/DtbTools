namespace DtbMerger2Library.Actions
{
    public interface IAction
    {
        void Execute();

        void UnExecute();

        bool CanExecute { get; }

        bool CanUnExecute { get; }

        string Description { get; }
    }
}
