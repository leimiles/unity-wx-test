using Cysharp.Threading.Tasks;
using System.Threading;

public interface IGameFlow
{
    UniTask RunAsync(CancellationToken ct);
}
