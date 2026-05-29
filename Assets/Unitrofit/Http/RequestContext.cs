using System.Collections.Generic;

namespace Unitrofit.Http
{
    /// <summary>
    /// 요청 인터셉터에 전달되는 읽기 전용 요청 컨텍스트.
    /// UnityWebRequest 내부 구조를 직접 노출하지 않고
    /// 로깅/인증 등에 필요한 정보만 담는다.
    /// </summary>
    public class RequestContext
    {
        /// <summary>HTTP 메서드 (GET, POST, PUT, …)</summary>
        public string Method { get; }

        /// <summary>전체 요청 URL (쿼리스트링 포함)</summary>
        public string Url { get; }

        /// <summary>요청에 적용된 헤더 목록</summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>요청 바디 (JSON 문자열). 바디가 없으면 null.</summary>
        public string Body { get; }

        /// <summary>바디가 존재하는지 여부</summary>
        public bool HasBody => Body != null;

        public RequestContext(
            string method,
            string url,
            Dictionary<string, string> headers,
            string body)
        {
            Method  = method;
            Url     = url;
            Headers = headers ?? new Dictionary<string, string>();
            Body    = body;
        }
    }
}
