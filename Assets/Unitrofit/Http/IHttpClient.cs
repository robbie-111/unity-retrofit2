using System.Threading;
using Cysharp.Threading.Tasks;
using Unitrofit.Core;

namespace Unitrofit.Http
{
    /// <summary>
    /// HTTP 전송 구현체 추상화.
    /// 인터셉터 체인은 <see cref="UnitrofitClient"/>가 담당하며,
    /// 타임아웃은 <see cref="UnitrofitClient.Builder.Build"/> 시점에 <see cref="SetTimeout"/>으로 주입된다.
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
        /// <summary>타임아웃은 구현체 생성 시 주입된다. 빌드 이후 변경 불가.</summary>
        void SetTimeout(int timeoutSeconds);

        UniTask<RawResponse> SendAsync(string url, RequestInfo info, CancellationToken ct);
    }
}
