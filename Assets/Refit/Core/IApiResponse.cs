using System.Collections.Generic;

namespace Refit.Core
{
    /// <summary>
    /// 바디 없는 API 응답 인터페이스. 상태코드·헤더·에러 정보만 포함한다.
    /// </summary>
    public interface IApiResponse
    {
        /// <summary>HTTP 상태 코드</summary>
        long StatusCode { get; }

        /// <summary>응답 헤더</summary>
        IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>2xx 범위이며 역직렬화 오류가 없으면 true</summary>
        bool IsSuccessful { get; }

        /// <summary>서버로부터 응답이 도착했으면 true (네트워크 오류이면 false)</summary>
        bool IsReceived { get; }

        /// <summary>에러 정보. 성공 시 null.</summary>
        ApiExceptionBase Error { get; }

        /// <summary>요청 실패(네트워크 오류) 여부를 확인하고 예외를 out 으로 반환한다.</summary>
        bool HasRequestError(out ApiRequestException error);

        /// <summary>응답 에러(4xx/5xx) 여부를 확인하고 예외를 out 으로 반환한다.</summary>
        bool HasResponseError(out ApiException error);
    }

    /// <summary>
    /// 역직렬화된 데이터를 포함하는 API 응답 인터페이스.
    /// </summary>
    public interface IApiResponse<out T> : IApiResponse
    {
        /// <summary>역직렬화된 응답 데이터. 실패 시 default(T).</summary>
        T Content { get; }
    }
}
