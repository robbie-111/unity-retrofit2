using System.Threading;
using Cysharp.Threading.Tasks;
using Unitrofit.Core;

namespace Unitrofit.Http
{
    /// <summary>
    /// HTTP 전송 구현체 추상화.
    /// 인터셉터 체인·타임아웃은 <see cref="UnitrofitClient"/>가 담당하며,
    /// 구현체는 순수 HTTP I/O만 수행한다.
    /// <para>
    /// UnitrofitClient.Builder에서 선택적으로 주입한다:
    /// <code>
    /// // UnityWebRequest (기본값 — 지정하지 않으면 자동 사용)
    /// new UnitrofitClient.Builder().Build();
    ///
    /// // System.Net.HttpClient 선택
    /// new UnitrofitClient.Builder()
    ///     .HttpClient(new NetHttpClientImpl())
    ///     .Build();
    /// </code>
    /// </para>
    /// </summary>
    public interface IHttpClient
    {
        UniTask<RawResponse> SendAsync(string url, RequestInfo info, int timeoutSeconds, CancellationToken ct);
    }
}
