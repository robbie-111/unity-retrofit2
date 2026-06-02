using Unitrofit.Http;

namespace Unitrofit.Interceptor
{
    /// <summary>
    /// HTTP 요청/응답을 가로채는 인터셉터.
    /// 인증 헤더 추가, 로깅, Retry, 공통 파라미터 삽입 등에 활용한다.
    /// <example>
    /// var client = new UnitrofitClient.Builder()
    ///     .AddInterceptor(new LoggingInterceptor())
    ///     .AddInterceptor(new AuthInterceptor())
    ///     .Build();
    /// </example>
    /// </summary>
    public interface IRequestInterceptor
    {
        /// <summary>
        /// 요청 전송 직전에 호출된다.
        /// method, url, headers, body 정보를 담은 RequestContext를 받는다.
        /// </summary>
        void OnRequest(RequestContext context);

        /// <summary>
        /// 응답 수신 직후에 호출된다. 로깅, 에러 트래킹, Retry 트리거 등에 활용.
        /// UnityWebRequestImpl / NetHttpClientImpl 모두 동일한 시그니처로 호출된다.
        /// </summary>
        void OnResponse(RawResponse response);
    }
}
