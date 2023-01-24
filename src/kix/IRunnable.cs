namespace Kix;

internal interface IRunnable : IRunOutput
{
    Task<int> RunAsync();
}
