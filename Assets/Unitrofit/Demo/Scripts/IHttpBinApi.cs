using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unitrofit.Attributes.Http;
using Unitrofit.Attributes.Params;
using Unitrofit.Core;

namespace Unitrofit.Demo
{
    // ── 응답 모델 ─────────────────────────────────────────────────────

    [System.Serializable]
    public class HttpBinGetResponse
    {
        public string url;
        public Dictionary<string, string> args;
        public Dictionary<string, string> headers;
    }

    [System.Serializable]
    public class HttpBinPostResponse
    {
        public string url;
        public string data;
        public Dictionary<string, string> form;
        public Dictionary<string, string> headers;
    }

    [System.Serializable]
    public class PostBody
    {
        public string name;
        public int    value;
    }

    // ── REST API 인터페이스 ─────────────────────────────────────────────

    /// <summary>
    /// Unitrofit 데모 인터페이스.
    /// 각 기능을 각 모듈(인증/도메인)이 자신의 헤더/쿼리를 직접 선언하는 방식으로 작성.
    /// </summary>
    [Headers("Accept: application/json", "User-Agent: Unitrofit")]
    public interface IHttpBinApi
    {
        // ── UniTask 반환 ──────────────────────────────────────────────

        // GET + Query
        [Get("/get")]
        UniTask<HttpBinGetResponse> GetAsync(
            [Query("query1")] string q1,
            [Query("query2")] string q2,
            CancellationToken ct = default);

        // GET + Path
        [Get("/delay/{seconds}")]
        UniTask<HttpBinGetResponse> GetDelayAsync(
            [Path("seconds")] int seconds,
            CancellationToken ct = default);

        // GET + QueryMap
        [Get("/get")]
        UniTask<HttpBinGetResponse> GetWithMapAsync(
            [QueryMap] Dictionary<string, string> filters,
            CancellationToken ct = default);

        // POST + JSON Body + 동적 헤더
        [Post("/post")]
        [Headers("X-Client: Unitrofit")]
        UniTask<HttpBinPostResponse> PostBodyAsync(
            [Body] PostBody body,
            [Header("Authorization")] string token,
            CancellationToken ct = default);

        // POST + Form 필드
        [Post("/post")]
        UniTask<HttpBinPostResponse> PostFormAsync(
            [Field("username")] string username,
            [Field("password")] string password,
            CancellationToken ct = default);

        // POST + Multipart
        [Multipart]
        [Post("/post")]
        UniTask<HttpBinPostResponse> UploadFileAsync(
            [Part("file")] MultipartBody file,
            [Field("description")] string description,
            CancellationToken ct = default);

        // PUT
        [Put("/put")]
        UniTask<HttpBinPostResponse> PutAsync(
            [Body] PostBody body,
            CancellationToken ct = default);

        // DELETE + AllowAnyStatusCode
        [Delete("/delete")]
        [AllowAnyStatusCode]
        UniTask<HttpBinGetResponse> DeleteAsync(
            [Query("id")] int id,
            CancellationToken ct = default);

        // ── Action 콜백 반환 ──────────────────────────────────────────

        [Get("/get")]
        void GetWithCallback(
            Action<HttpBinGetResponse> onSuccess,
            Action<UnitrofitException> onError,
            [Query("query1")] string q1,
            [Query("query2")] string q2);

        [Post("/post")]
        void PostWithCallback(
            Action<HttpBinPostResponse> onSuccess,
            Action<UnitrofitException> onError,
            [Body] PostBody body);
    }
}
