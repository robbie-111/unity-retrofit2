using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unitrofit.Adapter;
using Unitrofit.Core;

namespace Unitrofit.Demo
{
    /// <summary>
    /// IHttpBinApi 구현 서비스.
    /// UnitrofitAdapter.Builder로 생성한다:
    /// <code>
    /// var client = new UnitrofitClient.Builder()
    ///     .AddInterceptor(new LoggingInterceptor())
    ///     .Timeout(30)
    ///     .Build();
    ///
    /// var api = new UnitrofitAdapter.Builder()
    ///     .BaseUrl("https://httpbin.org")
    ///     .Client(client)
    ///     .Build&lt;HttpBinService&gt;(gameObject);
    /// </code>
    /// </summary>
    public class HttpBinService : UnitrofitAdapter, IHttpBinApi
    {
        public UniTask<HttpBinGetResponse> GetAsync(string q1, string q2, CancellationToken ct)
            => SendRequest<HttpBinGetResponse>(q1, q2, ct);

        public UniTask<HttpBinGetResponse> GetDelayAsync(int seconds, CancellationToken ct)
            => SendRequest<HttpBinGetResponse>(seconds, ct);

        public UniTask<HttpBinGetResponse> GetWithMapAsync(Dictionary<string, string> filters, CancellationToken ct)
            => SendRequest<HttpBinGetResponse>(filters, ct);

        public UniTask<HttpBinPostResponse> PostBodyAsync(PostBody body, string token, CancellationToken ct)
            => SendRequest<HttpBinPostResponse>(body, token, ct);

        public UniTask<HttpBinPostResponse> PostFormAsync(string username, string password, CancellationToken ct)
            => SendRequest<HttpBinPostResponse>(username, password, ct);

        public UniTask<HttpBinPostResponse> UploadFileAsync(MultipartBody file, string description, CancellationToken ct)
            => SendRequest<HttpBinPostResponse>(file, description, ct);

        public UniTask<HttpBinPostResponse> PutAsync(PostBody body, CancellationToken ct)
            => SendRequest<HttpBinPostResponse>(body, ct);

        public UniTask<HttpBinGetResponse> DeleteAsync(int id, CancellationToken ct)
            => SendRequest<HttpBinGetResponse>(id, ct);

        public void GetWithCallback(Action<HttpBinGetResponse> onSuccess, Action<UnitrofitException> onError, string q1, string q2)
            => SendRequest(onSuccess, onError, q1, q2);

        public void PostWithCallback(Action<HttpBinPostResponse> onSuccess, Action<UnitrofitException> onError, PostBody body)
            => SendRequest(onSuccess, onError, body);
    }
}
