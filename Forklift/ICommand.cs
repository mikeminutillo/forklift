namespace Forklift
{
    public interface ICommand
    {
        void Run(Args args);
    }
}