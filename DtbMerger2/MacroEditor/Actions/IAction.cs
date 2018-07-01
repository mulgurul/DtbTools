using System.Text;
using System.Threading.Tasks;

namespace MacroEditor.Actions
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
