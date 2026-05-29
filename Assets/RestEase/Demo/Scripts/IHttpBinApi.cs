using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestEase.Attributes.Http;
using RestEase.Attributes.Params;
using RestEase.Core;

namespace RestEase.Demo
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
    public class PostBody { public string name; public int value; }

    // ── REST API 인터페이스 ─────────────────────────────────────────────

    /// <summary>
    /// RestEase for Unity 데모 인터페이스.
    /// httpbin.org 를 대상으로 RestEase 원본의 주요 기능을 모두 시연한다.
    /// </summary>
    [BasePath("")]                               // 빈 BasePath (httpbin 은 prefix 없음)
    [Header("User-Agent", "RestEase-Unity")]     // 모든 요청에 공통 헤더
    [Header("Accept", "application/json")]
    public interface IHttpBinApi
    {
        // ── GET ──────────────────────────────────────────────────────

        /// Query 파라미터 + CancellationToken 자동 감지
        [Get("/get")]
        Task<HttpBinGetResponse> GetAsync(
            [Query("query1")] string q1,
            [Query("query2")] string q2,
            CancellationToken ct = default);

        /// Path 파라미터 + CancellationToken
        [Get("/delay/{seconds}")]
        Task<HttpBinGetResponse> GetDelayAsync(
            [Path("seconds")] int seconds,
            CancellationToken ct = default);

        /// QueryMap
        [Get("/get")]
        Task<HttpBinGetResponse> GetWithMapAsync(
            [QueryMap] Dictionary<string, string> filters,
            CancellationToken ct = default);

        // ── POST ─────────────────────────────────────────────────────

        /// JSON Body + 동적 헤더([HeaderParam])
        [Post("/post")]
        [Header("X-Source", "RestEase-Unity")]   // 메서드 레벨 정적 헤더
        Task<HttpBinPostResponse> PostBodyAsync(
            [Body] PostBody body,
            [HeaderParam("Authorization")] string token,
            CancellationToken ct = default);

        /// Form 필드 (application/x-www-form-urlencoded)
        [Post("/post")]
        Task<HttpBinPostResponse> PostFormAsync(
            [Field("username")] string username,
            [Field("password")] string password,
            CancellationToken ct = default);

        /// Multipart 파일 업로드
        [Multipart]
        [Post("/post")]
        Task<HttpBinPostResponse> UploadFileAsync(
            [Part("file")] FilePart file,
            [Field("description")] string description,
            CancellationToken ct = default);

        // ── PUT ──────────────────────────────────────────────────────

        [Put("/put")]
        Task<HttpBinPostResponse> PutAsync(
            [Body] PostBody body,
            CancellationToken ct = default);

        // ── DELETE + AllowAnyStatusCode ───────────────────────────────

        /// AllowAnyStatusCode: 비 2xx 도 예외 없이 Response<T> 로 반환
        [Delete("/delete")]
        [AllowAnyStatusCode]
        Task<Response<HttpBinGetResponse>> DeleteAsync(
            [Query("id")] int id,
            CancellationToken ct = default);

        // ── Response<T> 반환 예시 ─────────────────────────────────────

        /// 상태코드 + 역직렬화 데이터를 동시에 필요할 때
        [Get("/get")]
        Task<Response<HttpBinGetResponse>> GetWithResponseAsync(
            [Query("key")] string key,
            CancellationToken ct = default);
    }
}
