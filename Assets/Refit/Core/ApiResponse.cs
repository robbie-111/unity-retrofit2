using System.Collections.Generic;

namespace Refit.Core
{
    /// <summary>
    /// HTTP 응답의 모든 메타데이터를 포함하는 제네릭 래퍼.
    /// Task&lt;ApiResponse&lt;T&gt;&gt; 를 반환하면 예외 없이 상태코드를 직접 처리할 수 있다.
    ///
    /// <example>
    /// var response = await api.GetUserAsync("octocat");
    /// if (response.IsSuccessful)
    /// {
    ///     var user = response.Content;
    /// }
    /// else if (response.HasResponseError(out var err))
    /// {
    ///     Debug.LogError($"[{err.StatusCode}] {err.Message}");
    /// }
    /// </example>
    /// </summary>
    public class ApiResponse<T> : IApiResponse<T>
    {
        // ── IApiResponse 구현 ────────────────────────────────────────

        public long StatusCode { get; internal set; }
        public IReadOnlyDictionary<string, string> Headers { get; internal set; }
            = new Dictionary<string, string>();
        public bool IsSuccessful { get; internal set; }
        public bool IsReceived { get; internal set; }
        public ApiExceptionBase Error { get; internal set; }

        // ── IApiResponse<T> 구현 ─────────────────────────────────────

        public T Content { get; internal set; }

        // ── 원문 바디 ────────────────────────────────────────────────

        /// <summary>원문 응답 바디 (UTF-8 텍스트)</summary>
        public string RawBody { get; internal set; }

        // ── 헬퍼 메서드 ──────────────────────────────────────────────

        public bool HasRequestError(out ApiRequestException error)
        {
            error = Error as ApiRequestException;
            return error != null;
        }

        public bool HasResponseError(out ApiException error)
        {
            error = Error as ApiException;
            return error != null;
        }
    }
}
