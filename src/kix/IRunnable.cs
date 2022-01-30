namespace Kix;

internal interface IRunnable
{
    Task<int> RunAsync();
}
