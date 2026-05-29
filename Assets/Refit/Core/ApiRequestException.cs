using System;

namespace Refit.Core
{
    /// <summary>
    /// 서버에 요청 자체가 실패했을 때 throw 되는 예외.
    /// DNS 실패, 타임아웃, 네트워크 오류 등이 해당된다.
    /// <see cref="ApiException"/> 과 달리 HTTP 상태코드가 없다.
    /// </summary>
    public class ApiRequestException : ApiExceptionBase
    {
        public ApiRequestException(string message, string requestUri, Exception innerException = null)
            : base(message, requestUri, innerException) { }
    }
}
